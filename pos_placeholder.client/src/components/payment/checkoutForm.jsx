import {useStripe, useElements, PaymentElement} from '@stripe/react-stripe-js';
import * as orderApi from "@/api/orderApi.jsx";
import toastNotify from "@/helpers/toastNotify.js";

const CheckoutForm = ({paymentData, order, tip, onPaymentSuccess, isSplitPayment, partialAmount, onPartialPaymentSuccess}) => {
    const stripe = useStripe();
    const elements = useElements();

    const handleSubmit = async (event) => {
        event.preventDefault();

        if (!stripe || !elements) {
            return;
        }

        const result = await stripe.confirmPayment({
            elements,
            confirmParams: {
                return_url: "https://example.com/order/123/complete",
            },
            redirect: "if_required"
        });

        if (result.error) {
            toastNotify(result.error.message, "error");
        } else {

            if (isSplitPayment) {
                onPartialPaymentSuccess({
                    PaymentIntentId: paymentData.paymentIntentId,
                    GiftCardId: null,
                    Method: 0, // 0 -> "card", 1 -> "giftcard", 2 -> "cash"
                    PaidPrice: partialAmount
                });
            } else {
                const createOrderDto = {
                    Tip: tip ? Number(tip) : null,
                    OrderItems: order.products.map(item => ({
                        ProductVariationId: item.productVariationId,
                        Quantity: item.quantity
                    })),
                    OrderServiceIds: order.services.map(item => item.id),
                    PaymentIntentId: paymentData.paymentIntentId,
                    GiftCardId: null,
                    Method: 0 // 0 -> "card", 1 -> "giftcard", 2 -> "cash"
                };
                const createdOrder = await orderApi.createOrder(createOrderDto);
                onPaymentSuccess();
            }
        }
    };

    return (
        <form onSubmit={handleSubmit}>
            <PaymentElement/>
            <button className="btn btn-success mt-4 w-100" disabled={!stripe}>Pay</button>
        </form>
    );
};

export default CheckoutForm;