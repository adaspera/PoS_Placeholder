import React from 'react';
import {Elements} from '@stripe/react-stripe-js';
import {loadStripe} from '@stripe/stripe-js';
import CheckoutForm from "@/components/payment/checkoutForm.jsx";

const stripePromise = loadStripe('pk_test_51QUwSJAUD1o1wzrO8ARLfpK4CbH07GjmEaCQWJCJClmCHiu2WVTjTqZQfUKj8v94yaCv82TQe6Ebckz1Su2ZlGJK00Nwo8SyGQ');

const Payment = ({paymentData, order, tip, onPaymentSuccess}) => {
    const options = {
        // passing the client secret obtained from the server
        clientSecret: paymentData.clientSecret,
    };
    
    return (
        <Elements stripe={stripePromise} options={options}>
            <CheckoutForm paymentData={paymentData} order={order} tip={tip} onPaymentSuccess={onPaymentSuccess}/>
        </Elements>
    );
};

export default Payment;