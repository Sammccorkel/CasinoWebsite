using System;
using System.Threading.Tasks;

namespace Chuds2Chads.Services
{
    public class PokerSettlementService
    {
        private readonly WalletService _walletService;

        public PokerSettlementService(WalletService walletService)
        {
            _walletService = walletService;
        }

        public Task<bool> BuyInAsync(Guid userId, long buyInAmount, string reference)
        {
            return _walletService.TryPlaceBetAsync(userId, buyInAmount, reference);
        }

        public async Task<long> CashOutAsync(Guid userId, long endingStack, string reference)
        {
            if (endingStack < 0)
                throw new ArgumentOutOfRangeException(nameof(endingStack), "Ending stack cannot be negative.");

            if (endingStack == 0)
                return 0;

            await _walletService.CreditPayoutAsync(userId, endingStack, reference);
            return endingStack;
        }

        public long CalculateNetResult(long buyInAmount, long endingStack)
        {
            return endingStack - buyInAmount;
        }
    }
}