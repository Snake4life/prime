﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Prime.Common;
using Prime.Plugins.Services.Bit2c;
using Prime.Utility;

namespace Prime.Plugins.Services.Bit2C
{
    // https://www.bit2c.co.il/home/api
    public class Bit2CProvider : IPublicPricingProvider, IAssetPairsProvider
    {
        private const string Bit2CApiUrl = "https://www.bit2c.co.il/";

        private static readonly ObjectId IdHash = "prime:bit2c".GetObjectIdHashCode();

        //Nothing mentioned in documentation.
        private static readonly IRateLimiter Limiter = new NoRateLimits();

        private RestApiClientProvider<IBit2CApi> ApiProvider { get; }
        //From doc: BtcNis/LtcNis/BchNis - https://www.bit2c.co.il/home/api.
        private const string PairsCsv = "BtcNis,LtcNis,BchNis";

        public Network Network { get; } = Networks.I.Get("Bit2C");

        public bool Disabled => false;
        public int Priority => 100;
        public string AggregatorName => null;
        public string Title => Network.Name;
        public ObjectId Id => IdHash;
        public IRateLimiter RateLimiter => Limiter;
        public char? CommonPairSeparator { get; }

        public bool IsDirect => true;

        public ApiConfiguration GetApiConfiguration => ApiConfiguration.Standard2;

        public Bit2CProvider()
        {
            ApiProvider = new RestApiClientProvider<IBit2CApi>(Bit2CApiUrl, this, (k) => null);
        }

        private AssetPairs _pairs;
        public AssetPairs Pairs => _pairs ?? (_pairs = new AssetPairs(3, PairsCsv, this));

        public async Task<bool> TestPublicApiAsync(NetworkProviderContext context)
        {
            var api = ApiProvider.GetApi(context);
            var r = await api.GetTickerAsync("BtcNis").ConfigureAwait(false);

            return r != null;
        }

        public Task<AssetPairs> GetAssetPairsAsync(NetworkProviderContext context)
        {
            return Task.Run(() => Pairs);
        }

        public IAssetCodeConverter GetAssetCodeConverter()
        {
            return null;
        }

        private static readonly PricingFeatures StaticPricingFeatures = new PricingFeatures()
        {
            Single = new PricingSingleFeatures() { CanStatistics = true, CanVolume = true }
        };

        public PricingFeatures PricingFeatures => StaticPricingFeatures;

        public async Task<MarketPricesResult> GetPricingAsync(PublicPricesContext context)
        {
            var api = ApiProvider.GetApi(context);
            var pairCode = context.Pair.ToTicker(this, "");
            var r = await api.GetTickerAsync(pairCode).ConfigureAwait(false);
            
            return new MarketPricesResult(new MarketPrice(Network, context.Pair, r.ll)
            {
                PriceStatistics = new PriceStatistics(Network, context.Pair.Asset2, null, null, r.l, r.h),
                Volume = new NetworkPairVolume(Network, context.Pair, r.a)
            });
        }
    }
}