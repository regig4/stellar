using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StellarApp.Models;
using stellar_dotnet_sdk;
using stellar_dotnet_sdk.responses;

namespace StellarApp.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Create()
        {
            var pair = KeyPair.Random();
            string friendbotUrl = $"https://friendbot.stellar.org/?addr={pair.AccountId}";
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(friendbotUrl);
            return Ok($"Created Account information:\nAccountId: {pair.AccountId}" +
                $"\nPublic key: {pair.PublicKey}\nPrivate key: {pair.PrivateKey}\nSecret Seed: {pair.SecretSeed}" +
                $"\n{await response.Content.ReadAsStringAsync()}");
        }

        public async Task<IActionResult> Get(string accountId)
        {
            Server server = new Server("https://horizon-testnet.stellar.org");
            AccountResponse account = await server.Accounts.Account(accountId);
            AccountInformation result = new AccountInformation { Assets = new List<AccountInformation.AssetInformation>() };
            foreach (Balance balance in account.Balances)
                result.Assets.Add(new AccountInformation.AssetInformation
                {
                    Type = balance.AssetType,
                    Balance = balance.BalanceString,
                    Code = balance.AssetCode
                });
            return Json(result);
        }

        public async Task<IActionResult> CreateTrustline(string issuerSeed, string receiverSeed)
        {
            Network.UseTestNetwork();
            Server server = new Server("https://horizon-testnet.stellar.org");

            KeyPair issuingKeys = KeyPair.FromSecretSeed(issuerSeed);
            KeyPair receivingKeys = KeyPair.FromSecretSeed(receiverSeed);

            Asset eCoin = Asset.CreateNonNativeAsset("eCoin", issuingKeys.Address);

            AccountResponse receiving = await server.Accounts.Account(receivingKeys.Address);
            Transaction allowECoin = new Transaction.Builder(receiving)
              .AddOperation(new ChangeTrustOperation.Builder(eCoin, "1000").Build())
              .Build();
            allowECoin.Sign(receivingKeys);
            var response = await server.SubmitTransaction(allowECoin);

            if (response.IsSuccess())
                return Ok(response);
            else
                return BadRequest(response);
        }

        public async Task<IActionResult> Send(string issuerSeed, string receiverSeed, string amount)
        {
            Network.UseTestNetwork();
            Server server = new Server("https://horizon-testnet.stellar.org");

            KeyPair issuingKeys = KeyPair.FromSecretSeed(issuerSeed);
            KeyPair receivingKeys = KeyPair.FromSecretSeed(receiverSeed);

            Asset eCoin = Asset.CreateNonNativeAsset("eCoin", issuingKeys.Address);

            // Second, the issuing account actually sends a payment using the asset
            AccountResponse issuing = await server.Accounts.Account(issuingKeys.Address);
            Transaction sendECoin = new Transaction.Builder(issuing)
              .AddOperation(
                new PaymentOperation.Builder(receivingKeys, eCoin, amount).Build())
              .Build();
            sendECoin.Sign(issuingKeys);

            // And finally, send it off to Stellar!
            try
            {
                var response = await server.SubmitTransaction(sendECoin);
                return Ok(response);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
                // If the result is unknown (no response body, timeout etc.) we simply resubmit
                // already built transaction:
                // SubmitTransactionResponse response = server.submitTransaction(transaction);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
