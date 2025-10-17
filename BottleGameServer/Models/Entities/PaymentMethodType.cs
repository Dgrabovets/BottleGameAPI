using System.ComponentModel;

namespace BottleGameServer.Models.Entities
{
    /// <summary>
    /// Доступные методы пополнения
    /// </summary>
    public enum PaymentMethodType
    {
        /// <summary>
        /// СБП - Система быстрых платежей, RUB
        /// </summary>
        [Category(nameof(CurrencyType.RUB))]
        sbp_rub,

        /// <summary>
        /// Sberbank на карту, RUB
        /// </summary>
        [Category(nameof(CurrencyType.RUB))]
        sberbank_rub,

        /// <summary>
        /// Sberbank по номеру счёта, RUB
        /// </summary>
        [Category(nameof(CurrencyType.RUB))]
        sberbank_account_rub,

        /// <summary>
        /// Tinkoff Bank, RUB
        /// </summary>
        [Category(nameof(CurrencyType.RUB))]
        tinkoff_rub,

        /// <summary>
        /// Alfabank, RUB
        /// </summary>
        [Category(nameof(CurrencyType.RUB))]
        alfabank_rub,

        /// <summary>
        /// Raiffeisen, RUB
        /// </summary>
        [Category(nameof(CurrencyType.RUB))]
        raiffeisen_rub,

        /// <summary>
        /// VTB Bank, RUB
        /// </summary>
        [Category(nameof(CurrencyType.RUB))]
        vtb_rub,

        /// <summary>
        /// RNKB Bank, RUB
        /// </summary>
        [Category(nameof(CurrencyType.RUB))]
        rnkbbank_rub,

        /// <summary>
        /// Pochta bank, RUB
        /// </summary>
        [Category(nameof(CurrencyType.RUB))]
        postbank_rub,

        /// <summary>
        /// YooMoney, RUB
        /// </summary>
        [Category(nameof(CurrencyType.RUB))]
        yoomoney_rub,

        /// <summary>
        /// AdvCash, RUB
        /// </summary>
        [Category(nameof(CurrencyType.RUB))]
        advcash_rub,

        /// <summary>
        /// Payeer, RUB
        /// </summary>
        [Category(nameof(CurrencyType.RUB))]
        payeer_rub,

        /// <summary>
        /// NiceWallet, USDT (криптовалюта)
        /// </summary>
        [Category(nameof(CurrencyType.USDT))]
        nicewallet_usdt,

        /// <summary>
        /// Monobank, UAH
        /// </summary>
        [Category(nameof(CurrencyType.UAH))]
        monobank_uah,

        /// <summary>
        /// Privat Bank, UAH
        /// </summary>
        [Category(nameof(CurrencyType.UAH))]
        privatbank_uah,

        /// <summary>
        /// Raiffeisen, UAH
        /// </summary>
        [Category(nameof(CurrencyType.UAH))]
        raiffeisen_uah,

        /// <summary>
        /// AdvCash, KZT
        /// </summary>
        [Category(nameof(CurrencyType.KZT))]
        advcash_kzt,

        /// <summary>
        /// Kaspi Bank, KZT
        /// </summary>
        [Category(nameof(CurrencyType.KZT))]
        kaspibank_kzt,

        /// <summary>
        /// Halyk Bank, KZT
        /// </summary>
        [Category(nameof(CurrencyType.KZT))]
        halykbank_kzt,

        /// <summary>
        /// Jysan Bank, KZT
        /// </summary>
        [Category(nameof(CurrencyType.KZT))]
        jysanbank_kzt,

        /// <summary>
        /// CenterCredit Bank, KZT
        /// </summary>
        [Category(nameof(CurrencyType.KZT))]
        centercreditbank_kzt,

        /// <summary>
        /// ForteBank, KZT
        /// </summary>
        [Category(nameof(CurrencyType.KZT))]
        fortebank_kzt,

        /// <summary>
        /// AdvCash, USD
        /// </summary>
        [Category(nameof(CurrencyType.USD))]
        advcash_usd,

        /// <summary>
        /// Payeer, USD
        /// </summary>
        [Category(nameof(CurrencyType.USD))]
        payeer_usd,

        /// <summary>
        /// PayPal, USD
        /// </summary>
        [Category(nameof(CurrencyType.USD))]
        paypal_usd,

        /// <summary>
        /// AdvCash, EUR
        /// </summary>
        [Category(nameof(CurrencyType.EUR))]
        advcash_eur,

        /// <summary>
        /// Payeer, EUR
        /// </summary>
        [Category(nameof(CurrencyType.EUR))]
        payeer_eur,

        /// <summary>
        /// PayPal, EUR
        /// </summary>
        [Category(nameof(CurrencyType.EUR))]
        paypal_eur
    }
}