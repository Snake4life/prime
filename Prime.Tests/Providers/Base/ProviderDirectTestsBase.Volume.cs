﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nito.AsyncEx;
using Prime.Common;
using Prime.Utility;

namespace Prime.Tests.Providers
{
    public abstract partial class ProviderDirectTestsBase
    {
        public virtual void TestGetVolume() { }
        public void TestGetVolume(List<AssetPair> pairs, bool firstVolumeBaseBiggerThanQuote)
        {
            var p = IsType<IPublicVolumeProvider>();
            if (p.Success)
                GetVolume(p.Provider, pairs, firstVolumeBaseBiggerThanQuote);
        }

        private void InternalGetVolumeAsync(IPublicVolumeProvider provider, PublicVolumesContext context, bool firstVolumeBaseBiggerThanQuote, bool runSingle)
        {
            Assert.IsTrue(provider.VolumeFeatures != null, "Volume features object is null");

            var r = AsyncContext.Run(() => provider.GetPublicVolumeAsync(context));

            // First volume, IsRequestAll.
            var volume = r.Volume.FirstOrDefault();
            Assert.IsTrue(volume != null, "Provider returned no volume records");

            if (!context.IsRequestAll)
            {
                var dist = r.Volume.DistinctBy(x => x.Pair).ToList();
                Assert.IsTrue(context.Pairs.Count == dist.Count, "Provider didn't return required pairs");

                Assert.IsTrue(volume.Pair.Asset1.Equals(context.Pair.Asset1), "Incorrect base asset");
                Assert.IsTrue(volume.Pair.Asset2.Equals(context.Pair.Asset2), "Incorrect quote asset");
            }

            // First volume, base/quote volumes relation.
            if (volume.HasVolume24Base && volume.HasVolume24Quote)
            {
                if (firstVolumeBaseBiggerThanQuote)
                    Assert.IsTrue(volume.Volume24Base.ToDecimal(null) > volume.Volume24Quote.ToDecimal(null), "Quote volume is bigger than base (within volume)");
                else
                    Assert.IsTrue(volume.Volume24Base.ToDecimal(null) < volume.Volume24Quote.ToDecimal(null), "Base volume is bigger than quote (within volume)");
            }

            // All volume.
            foreach (var networkPairVolume in r.Volume)
            {
                if (networkPairVolume.HasVolume24Base)
                {
                    Assert.IsFalse(networkPairVolume.Volume24Base.ToDecimal(null) == 0,
                        $"Base volume of {networkPairVolume.Pair} is 0");
                    Trace.WriteLine($"Base volume for {networkPairVolume.Pair} pair is {networkPairVolume.Volume24Base}");
                }

                if (networkPairVolume.HasVolume24Quote)
                {
                    Assert.IsFalse(networkPairVolume.Volume24Quote.ToDecimal(null) == 0,
                        $"Quote volume of {networkPairVolume.Pair} is 0");
                    Trace.WriteLine($"Quote volume for {networkPairVolume.Pair} pair is {networkPairVolume.Volume24Quote}");
                }

                if (networkPairVolume.HasVolume24Base && networkPairVolume.HasVolume24Quote)
                {
                    Assert.IsTrue(volume.Volume24Base.ToDecimal(null) != volume.Volume24Quote.ToDecimal(null), "Base and quote volume are the same");
                }
            }
        }

        private void GetVolume(IPublicVolumeProvider provider, List<AssetPair> pairs, bool volumeBaseBiggerThanQuote)
        {
            try
            {
                Trace.WriteLine("Volume interface test\n\n");

                if (provider.VolumeFeatures.HasSingle)
                {
                    Trace.WriteLine("\nSingle features test\n");

                    var context = new PublicVolumeContext(pairs.First());

                    InternalGetVolumeAsync(provider, context, volumeBaseBiggerThanQuote, true);
                }

                if (provider.VolumeFeatures.HasBulk)
                {
                    Trace.WriteLine("\nBulk features test with pairs selection\n");
                    var context = new PublicVolumesContext(pairs);

                    InternalGetVolumeAsync(provider, context, volumeBaseBiggerThanQuote, false);

                    if (provider.VolumeFeatures.Bulk.CanReturnAll)
                    {
                        Trace.WriteLine("\nBulk features test (provider can return all volumes)\n");
                        context = new PublicVolumesContext();

                        InternalGetVolumeAsync(provider, context, volumeBaseBiggerThanQuote, false);
                    }
                }

                TestVolumePricingSanity(provider, pairs);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

    }
}
