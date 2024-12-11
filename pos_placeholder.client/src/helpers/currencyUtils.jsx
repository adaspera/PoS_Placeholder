export function getCurrency () {
    const currencyCode = localStorage.getItem("currency");

    let currency;
    switch (currencyCode) {
        case "EUR":
            currency = "€";
            break;
        case "USD":
            currency = "$";
            break;
        default:
            currency = "$";
    }

    return currency;
}