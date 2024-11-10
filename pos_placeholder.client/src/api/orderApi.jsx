
export const getOrder = (id) => {
    //const OrderDTO = fetch("https://",{method: "GET"})
    //    .then((response) => response.json())
    const OrderDTO = null;

    return OrderDTO;
}

export const createOrder = () => {
    //const OrderDTO = fetch("https://",{method: "GET"})
    //    .then((response) => response.json())
    const OrderDTO = {
        products: [
            { id: 1, fullName: "Item 1", price: 10.99, quantity: 2 },
            { id: 2, fullName: "Item 2", price: 15.49, quantity: 1 },
            { id: 3, fullName: "Item 3", price: 7.99, quantity: 3 },
            { id: 4, fullName: "Item 4", price: 12.99, quantity: 1 },
            { id: 5, fullName: "Item 5", price: 4.99, quantity: 5 }
        ]
    };
    return OrderDTO;
}