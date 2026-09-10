using System;
using System.Threading.Tasks;

namespace Chuds2Chads.Services
{
    public enum BlackjackSettlementOutcome
    {
        DealerWin,
        PlayerWin,
        Push
    }

    public class BlackjackSettlementService
    {
        private readonly WalletService _walletService;

        public BlackjackSettlementService(WalletService walletService)
        {
            _walletService = walletService;
        }

        public Task<bool> PlaceBetAsync(Guid userId, long stake, string reference)
        {
            return _walletService.TryPlaceBetAsync(userId, stake, reference);
        }

        public async Task<long> ResolveRoundAsync(
            Guid userId,
            long stake,
            BlackjackSettlementOutcome outcome,
            string payoutReference)
        {
            long payout = outcome switch
            {
                BlackjackSettlementOutcome.PlayerWin => stake * 2,
                BlackjackSettlementOutcome.Push => stake,
                _ => 0
            };

            if (payout > 0)
            {
                await _walletService.CreditPayoutAsync(userId, payout, payoutReference);
            }

            return payout;
        }
    }
}