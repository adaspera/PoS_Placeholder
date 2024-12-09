import { apiService } from "./ApiService";

export const getOrder = (id) => {
    //const OrderDTO = fetch("https://",{method: "GET"})
    //    .then((response) => response.json())
    const OrderDTO = null;

    return OrderDTO;
}

export const createOrder = async (products) => {
    try {
        return await apiService.post("/api/orders", products);
    } catch (e) {
        console.log("Error creating order:", e);
    }
}

