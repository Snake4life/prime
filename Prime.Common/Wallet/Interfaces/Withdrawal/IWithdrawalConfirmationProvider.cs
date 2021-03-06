﻿using System.Threading.Tasks;
using Prime.Common.Wallet.Withdrawal.Confirmation;

namespace Prime.Common
{
    public interface IWithdrawalConfirmationProvider
    {
        Task<WithdrawalConfirmationResult> ConfirmWithdrawalAsync(WithdrawalConfirmationContext context);
    }
}