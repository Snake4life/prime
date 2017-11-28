﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Prime.Plugins.Services.EthexIndia;
using RestEase;

namespace Prime.Plugins.Services.LiveCoin
{
    internal interface ILiveCoinApi
    {
        [Get("/ticker?currencyPair={currencyPair}")]
        Task<LiveCoinSchema.TickerResponse> GetTickerAsync([Path] string currencyPair);

        [Get("/ticker")]
        Task<LiveCoinSchema.TickerResponse[]> GetTickersAsync();
    }
}
