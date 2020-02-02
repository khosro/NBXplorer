using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBXplorer.DerivationStrategy;
using NBXplorer.Logging;
using NBXplorer.ModelBinders;
using NBXplorer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace NBXplorer.Controllers
{
	public partial class MainController
	{
		#region Main Version

		[HttpPost]
		[Route("cryptos/{network}/psbt/create")]
		public async Task<IActionResult> CreatePSBTs(
			[ModelBinder(BinderType = typeof(NetworkModelBinder))]
			NBXplorerNetwork network,
			[FromBody]
			JObject body)
		{
			if (body == null)
				throw new ArgumentNullException(nameof(body));

			IEnumerable<CreatePSBTRequest1> requests = ParseJObject<CreatePSBTRequest1s>(body, network).Requests;

			TransactionBuilder txBuilder = null;

			#region 
			foreach (var request in requests)
			{
				txBuilder = await Transaction(request, network, txBuilder);
			}

			CreatePSBTRequest1 request1 = requests.FirstOrDefault();//TODO.All value for calculating Fee must be the same, otherwise throw exception.
			await Fee(request1, txBuilder, network.CryptoCode);

			var psbt = txBuilder.BuildPSBT(false);

			foreach (var request in requests)
			{
				await UpdatePSBTCore(new UpdatePSBTRequest()
				{
					DerivationScheme = request.Strategy,
					PSBT = psbt,
					RebaseKeyPaths = request.RebaseKeyPaths
				}, network);
			}

			var resp = new CreatePSBTResponse()
			{
				PSBT = psbt,
				//ChangeAddress = hasChange ? change.ScriptPubKey.GetDestinationAddress(network.NBitcoinNetwork) : null
			};
			#endregion

			return Json(resp, network.JsonSerializerSettings);


			//return await Transaction(requests.FirstOrDefault(), network, txBuilder);
		}

		async
			Task<TransactionBuilder>
			//Task<JsonResult>
			Transaction(CreatePSBTRequest1 request, NBXplorerNetwork network, TransactionBuilder txBuilder)
		{
			/*
			 * TODO.This code actually copied from (MainController.PSBT).CreatePSBT
			 * If that code has changed then we must change the following code.If any line changed from that code i mentioned it by "Changed by Khosro".
			 * And if added the code, i mentioned it by "Added by Khosro".
			 * And somewhere i added comment with the title "Khosro Comment".
			 */

			var strategy = request.Strategy;

			var repo = RepositoryProvider.GetRepository(network);

			if (txBuilder == null)//Added by Khosro
			{
				/*var Changed by Khosro*/
				txBuilder = request.Seed is int s ? network.NBitcoinNetwork.CreateTransactionBuilder(s)
													: network.NBitcoinNetwork.CreateTransactionBuilder();
			}
			else//Added by Khosro
			{
				txBuilder = txBuilder.Then(); //Added by Khosro
			}


			if (Waiters.GetWaiter(network).NetworkInfo?.GetRelayFee() is FeeRate feeRate)
			{
				txBuilder.StandardTransactionPolicy.MinRelayTxFee = feeRate;
			}

			txBuilder.OptInRBF = request.RBF;
			if (request.LockTime is LockTime lockTime)
			{
				txBuilder.SetLockTime(lockTime);
				txBuilder.OptInRBF = true;
			}
			var utxos = (await GetUTXOs(network.CryptoCode, strategy, null)).GetUnspentCoins(request.MinConfirmations);
			var availableCoinsByOutpoint = utxos.ToDictionary(o => o.Outpoint);
			if (request.IncludeOnlyOutpoints != null)
			{
				var includeOnlyOutpoints = request.IncludeOnlyOutpoints.ToHashSet();
				availableCoinsByOutpoint = availableCoinsByOutpoint.Where(c => includeOnlyOutpoints.Contains(c.Key)).ToDictionary(o => o.Key, o => o.Value);
			}

			if (request.ExcludeOutpoints?.Any() is true)
			{
				var excludedOutpoints = request.ExcludeOutpoints.ToHashSet();
				availableCoinsByOutpoint = availableCoinsByOutpoint.Where(c => !excludedOutpoints.Contains(c.Key)).ToDictionary(o => o.Key, o => o.Value);
			}
			txBuilder.AddCoins(availableCoinsByOutpoint.Values);

			foreach (var dest in request.Destinations)
			{
				if (dest.SweepAll)
				{
					txBuilder.SendAll(dest.Destination);
				}
				else
				{
					txBuilder.Send(dest.Destination, dest.Amount);
					if (dest.SubstractFees)
						txBuilder.SubtractFees();
				}
			}
			(Script ScriptPubKey, KeyPath KeyPath) change = (null, null);

			bool hasChange = false;

			// We first build the transaction with a change which keep the length of the expected change scriptPubKey
			// This allow us to detect if there is a change later in the constructed transaction.
			// This defend against bug which can happen if one of the destination is the same as the expected change
			// This assume that a script with only 0 can't be created from a strategy, nor by passing any data to explicitChangeAddress
			if (request.ExplicitChangeAddress == null)
			{
				// The dummyScriptPubKey is necessary to know the size of the change
				var dummyScriptPubKey = utxos.FirstOrDefault()?.ScriptPubKey ?? strategy.GetDerivation(0).ScriptPubKey;
				change = (Script.FromBytesUnsafe(new byte[dummyScriptPubKey.Length]), null);
			}
			else
			{
				change = (Script.FromBytesUnsafe(new byte[request.ExplicitChangeAddress.ScriptPubKey.Length]), null);
			}
			txBuilder.SetChange(change.ScriptPubKey);
			PSBT psbt = null;
			try
			{
				#region Fee region commented Khosro Changed
				/*
				 Comment#1 Khosro Comment.We move it to MainController.CreatePSBTs

				if (request.FeePreference?.ExplicitFeeRate is FeeRate explicitFeeRate)
				{
					txBuilder.SendEstimatedFees(explicitFeeRate);
				}
				else if (request.FeePreference?.BlockTarget is int blockTarget)
				{
					try
					{
						var rate = await GetFeeRate(blockTarget, network.CryptoCode);
						txBuilder.SendEstimatedFees(rate.FeeRate);
					}
					catch (NBXplorerException e) when (e.Error.Code == "fee-estimation-unavailable" && request.FeePreference?.FallbackFeeRate is FeeRate fallbackFeeRate)
					{
						txBuilder.SendEstimatedFees(fallbackFeeRate);
					}
				}
				else if (request.FeePreference?.ExplicitFee is Money explicitFee)
				{
					txBuilder.SendFees(explicitFee);
				}
				else
				{
					try
					{
						var rate = await GetFeeRate(1, network.CryptoCode);
						txBuilder.SendEstimatedFees(rate.FeeRate);
					}
					catch (NBXplorerException e) when (e.Error.Code == "fee-estimation-unavailable" && request.FeePreference?.FallbackFeeRate is FeeRate fallbackFeeRate)
					{
						txBuilder.SendEstimatedFees(fallbackFeeRate);
					}
				}*/
				#endregion
				psbt = txBuilder.BuildPSBT(false);
				hasChange = psbt.Outputs.Any(o => o.ScriptPubKey == change.ScriptPubKey);
			}
			catch (NotEnoughFundsException)
			{
				throw new NBXplorerException(new NBXplorerError(400, "not-enough-funds", "Not enough funds for doing this transaction"));
			}
			if (hasChange) // We need to reserve an address, so we need to build again the psbt
			{
				if (request.ExplicitChangeAddress == null)
				{
					var derivation = await repo.GetUnused(strategy, DerivationFeature.Change, 0, request.ReserveChangeAddress);
					change = (derivation.ScriptPubKey, derivation.KeyPath);
				}
				else
				{
					change = (request.ExplicitChangeAddress.ScriptPubKey, null);
				}
				txBuilder.SetChange(change.ScriptPubKey);
				psbt = txBuilder.BuildPSBT(false);
			}

			var tx = psbt.GetOriginalTransaction();
			if (request.Version is uint v)
				tx.Version = v;

			#region Changed by Khosro Commented before
			/* Changed by Khosro*/
			/*	psbt = txBuilder.CreatePSBTFrom(tx, false, SigHash.All);

				// Maybe it is a change that we know about, let's search in the DB
				if (hasChange && change.KeyPath == null)
				{
					var keyInfos = await repo.GetKeyInformations(new[] { request.ExplicitChangeAddress.ScriptPubKey });
					if (keyInfos.TryGetValue(request.ExplicitChangeAddress.ScriptPubKey, out var kis))
					{
						var ki = kis.FirstOrDefault(k => k.DerivationStrategy == strategy);
						if (ki != null)
							change = (change.ScriptPubKey, kis.First().KeyPath);
					}
				}

				//Khosro Comment. This is actaully move to CreatePSBTs method in this class

				await UpdatePSBTCore(new UpdatePSBTRequest()
				{
					DerivationScheme = strategy,
					PSBT = psbt,
					RebaseKeyPaths = request.RebaseKeyPaths
				}, network);

				var resp = new CreatePSBTResponse()
				{
					PSBT = psbt,
					ChangeAddress = hasChange ? change.ScriptPubKey.GetDestinationAddress(network.NBitcoinNetwork) : null
				};

				return Json(resp, network.JsonSerializerSettings);
				*/
			#endregion


			return txBuilder;//Added by Khosro
		}


		async Task Fee(CreatePSBTRequest1 request, TransactionBuilder txBuilder, string cryptoCode)
		{
			if (request.FeePreference?.ExplicitFeeRate is FeeRate explicitFeeRate)
			{
				txBuilder.SendEstimatedFees(explicitFeeRate);
			}
			else if (request.FeePreference?.BlockTarget is int blockTarget)
			{
				try
				{
					var rate = await GetFeeRate(blockTarget, cryptoCode);
					txBuilder.SendEstimatedFees(rate.FeeRate);
				}
				catch (NBXplorerException e) when (e.Error.Code == "fee-estimation-unavailable" && request.FeePreference?.FallbackFeeRate is FeeRate fallbackFeeRate)
				{
					txBuilder.SendEstimatedFees(fallbackFeeRate);
				}
			}
			else if (request.FeePreference?.ExplicitFee is Money explicitFee)
			{
				txBuilder.SendFees(explicitFee);
			}
			else
			{
				try
				{
					var rate = await GetFeeRate(1, cryptoCode);
					txBuilder.SendEstimatedFees(rate.FeeRate);
				}
				catch (NBXplorerException e) when (e.Error.Code == "fee-estimation-unavailable" && request.FeePreference?.FallbackFeeRate is FeeRate fallbackFeeRate)
				{
					txBuilder.SendEstimatedFees(fallbackFeeRate);
				}
			}
		}
		#endregion

		#region  Another version Worked version
		[HttpPost]
		[Route("cryptos/{network}/psbt/create1")]
		public async Task<IActionResult> CreatePSBTs1(
			   [ModelBinder(BinderType = typeof(NetworkModelBinder))]
			NBXplorerNetwork network,
			   [ModelBinder(BinderType = typeof(DerivationStrategyModelBinder))]
			DerivationStrategyBase strategy,
			   [FromBody]
			JObject body)
		{
			if (body == null)
				throw new ArgumentNullException(nameof(body));

			IEnumerable<CreatePSBTRequest1> requests = ParseJObject<CreatePSBTRequest1s>(body, network).Requests;

			TransactionBuilder txBuilder = null;

			var resp = await Transaction1(requests.FirstOrDefault(), network, txBuilder);

			return Json(resp, network.JsonSerializerSettings);
		}

		async Task<CreatePSBTResponse> Transaction1(CreatePSBTRequest1 request, NBXplorerNetwork network, TransactionBuilder txBuilder)
		{
			var strategy = request.Strategy;

			var repo = RepositoryProvider.GetRepository(network);
			if (txBuilder == null)
			{
				txBuilder = request.Seed is int s ? network.NBitcoinNetwork.CreateTransactionBuilder(s)
												 : network.NBitcoinNetwork.CreateTransactionBuilder();
			}
			else
			{
				txBuilder = txBuilder.Then();
			}
			if (Waiters.GetWaiter(network).NetworkInfo?.GetRelayFee() is FeeRate feeRate)
			{
				txBuilder.StandardTransactionPolicy.MinRelayTxFee = feeRate;
			}

			txBuilder.OptInRBF = request.RBF;
			if (request.LockTime is LockTime lockTime)
			{
				txBuilder.SetLockTime(lockTime);
				txBuilder.OptInRBF = true;
			}
			var utxos = (await GetUTXOs(network.CryptoCode, strategy, null)).GetUnspentCoins(request.MinConfirmations);
			var availableCoinsByOutpoint = utxos.ToDictionary(o => o.Outpoint);
			if (request.IncludeOnlyOutpoints != null)
			{
				var includeOnlyOutpoints = request.IncludeOnlyOutpoints.ToHashSet();
				availableCoinsByOutpoint = availableCoinsByOutpoint.Where(c => includeOnlyOutpoints.Contains(c.Key)).ToDictionary(o => o.Key, o => o.Value);
			}

			if (request.ExcludeOutpoints?.Any() is true)
			{
				var excludedOutpoints = request.ExcludeOutpoints.ToHashSet();
				availableCoinsByOutpoint = availableCoinsByOutpoint.Where(c => !excludedOutpoints.Contains(c.Key)).ToDictionary(o => o.Key, o => o.Value);
			}
			txBuilder.AddCoins(availableCoinsByOutpoint.Values);

			foreach (var dest in request.Destinations)
			{
				if (dest.SweepAll)
				{
					txBuilder.SendAll(dest.Destination);
				}
				else
				{
					txBuilder.Send(dest.Destination, dest.Amount);
					if (dest.SubstractFees)
						txBuilder.SubtractFees();
				}
			}
			(Script ScriptPubKey, KeyPath KeyPath) change = (null, null);
			bool hasChange = false;
			// We first build the transaction with a change which keep the length of the expected change scriptPubKey
			// This allow us to detect if there is a change later in the constructed transaction.
			// This defend against bug which can happen if one of the destination is the same as the expected change
			// This assume that a script with only 0 can't be created from a strategy, nor by passing any data to explicitChangeAddress
			if (request.ExplicitChangeAddress == null)
			{
				// The dummyScriptPubKey is necessary to know the size of the change
				var dummyScriptPubKey = utxos.FirstOrDefault()?.ScriptPubKey ?? strategy.GetDerivation(0).ScriptPubKey;
				change = (Script.FromBytesUnsafe(new byte[dummyScriptPubKey.Length]), null);
			}
			else
			{
				change = (Script.FromBytesUnsafe(new byte[request.ExplicitChangeAddress.ScriptPubKey.Length]), null);
			}
			txBuilder.SetChange(change.ScriptPubKey);
			PSBT psbt = null;
			try
			{
				if (request.FeePreference?.ExplicitFeeRate is FeeRate explicitFeeRate)
				{
					txBuilder.SendEstimatedFees(explicitFeeRate);
				}
				else if (request.FeePreference?.BlockTarget is int blockTarget)
				{
					try
					{
						var rate = await GetFeeRate(blockTarget, network.CryptoCode);
						txBuilder.SendEstimatedFees(rate.FeeRate);
					}
					catch (NBXplorerException e) when (e.Error.Code == "fee-estimation-unavailable" && request.FeePreference?.FallbackFeeRate is FeeRate fallbackFeeRate)
					{
						txBuilder.SendEstimatedFees(fallbackFeeRate);
					}
				}
				else if (request.FeePreference?.ExplicitFee is Money explicitFee)
				{
					txBuilder.SendFees(explicitFee);
				}
				else
				{
					try
					{
						var rate = await GetFeeRate(1, network.CryptoCode);
						txBuilder.SendEstimatedFees(rate.FeeRate);
					}
					catch (NBXplorerException e) when (e.Error.Code == "fee-estimation-unavailable" && request.FeePreference?.FallbackFeeRate is FeeRate fallbackFeeRate)
					{
						txBuilder.SendEstimatedFees(fallbackFeeRate);
					}
				}
				psbt = txBuilder.BuildPSBT(false);
				hasChange = psbt.Outputs.Any(o => o.ScriptPubKey == change.ScriptPubKey);
			}
			catch (NotEnoughFundsException)
			{
				throw new NBXplorerException(new NBXplorerError(400, "not-enough-funds", "Not enough funds for doing this transaction"));
			}
			if (hasChange) // We need to reserve an address, so we need to build again the psbt
			{
				if (request.ExplicitChangeAddress == null)
				{
					var derivation = await repo.GetUnused(strategy, DerivationFeature.Change, 0, request.ReserveChangeAddress);
					change = (derivation.ScriptPubKey, derivation.KeyPath);
				}
				else
				{
					change = (request.ExplicitChangeAddress.ScriptPubKey, null);
				}
				txBuilder.SetChange(change.ScriptPubKey);
				psbt = txBuilder.BuildPSBT(false);
			}

			var tx = psbt.GetOriginalTransaction();
			if (request.Version is uint v)
				tx.Version = v;
			psbt = txBuilder.CreatePSBTFrom(tx, false, SigHash.All);

			// Maybe it is a change that we know about, let's search in the DB
			if (hasChange && change.KeyPath == null)
			{
				var keyInfos = await repo.GetKeyInformations(new[] { request.ExplicitChangeAddress.ScriptPubKey });
				if (keyInfos.TryGetValue(request.ExplicitChangeAddress.ScriptPubKey, out var kis))
				{
					var ki = kis.FirstOrDefault(k => k.DerivationStrategy == strategy);
					if (ki != null)
						change = (change.ScriptPubKey, kis.First().KeyPath);
				}
			}

			await UpdatePSBTCore(new UpdatePSBTRequest()
			{
				DerivationScheme = strategy,
				PSBT = psbt,
				RebaseKeyPaths = request.RebaseKeyPaths
			}, network);

			var resp = new CreatePSBTResponse()
			{
				PSBT = psbt,
				ChangeAddress = hasChange ? change.ScriptPubKey.GetDestinationAddress(network.NBitcoinNetwork) : null
			};
			return resp;
		}

		#endregion
	}
}