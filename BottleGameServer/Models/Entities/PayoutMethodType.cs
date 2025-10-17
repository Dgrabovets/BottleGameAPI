using System.ComponentModel;

namespace BottleGameServer.Models.Entities
{
    /// <summary>
    /// Доступные методы выплат
    /// </summary>
    public enum PayoutMethodType
    {
        /// <summary>
        /// Банковская карта РФ, RUB
        /// </summary>
        [Category(nameof(CurrencyType.RUB))]
        bankcard_rub,

        /// <summary>
        /// СБП - Система быстрых платежей, RUB
        /// </summary>
        [Category(nameof(CurrencyType.RUB))]
        sbp_rub,

        /// <summary>
        /// Банковская карта Казахстан, KZT
        /// </summary>
        [Category(nameof(CurrencyType.KZT))]
        bankcard_kzt,

        /// <summary>
        /// YooMoney RUB
        /// </summary>
        [Category(nameof(CurrencyType.RUB))]
        yoomoney_rub,

        /// <summary>
        /// AdvCash RUB
        /// </summary>
        [Category(nameof(CurrencyType.RUB))]
        advcash_rub,

        /// <summary>
        /// AdvCash USD
        /// </summary>
        [Category(nameof(CurrencyType.USD))]
        advcash_usd,

        /// <summary>
        /// AdvCash EUR
        /// </summary>
        [Category(nameof(CurrencyType.EUR))]
        advcash_eur,

        /// <summary>
        /// AdvCash KZT
        /// </summary>
        [Category(nameof(CurrencyType.KZT))]
        advcash_kzt,

        /// <summary>
        /// Payeer RUB
        /// </summary>
        [Category(nameof(CurrencyType.RUB))]
        payeer_rub,

        /// <summary>
        /// Payeer USD
        /// </summary>
        [Category(nameof(CurrencyType.USD))]
        payeer_usd,

        /// <summary>
        /// Payeer EUR
        /// </summary>
        [Category(nameof(CurrencyType.EUR))]
        payeer_eur
    }
}