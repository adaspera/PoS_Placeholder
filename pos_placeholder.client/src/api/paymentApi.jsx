import { apiService } from "./ApiService";

export const makePayment = async (paymentRequest) => {
    try {
        return await apiService.post("/api/payments/makepayment", paymentRequest);
    } catch (e) {
        console.log("Error making payment:", e);
    }
}