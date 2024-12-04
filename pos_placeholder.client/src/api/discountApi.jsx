import { apiService } from "./ApiService";

// export const getDiscounts = async () => {
//     try {
//         return await apiService.get("/api/discounts");
//     } catch (e) {
//         console.error("Error fetching discounts:", e);
//     }
// };

export const getDiscounts = async () => {
    const discountsDTO = [
        {
            id: 0,
            amount: 15,
            startDate: "2024-1-2",
            endDate: "2024-5-2",
            isPercentage: true
        },
        {
            id: 1,
            amount: 10,
            startDate: "2024-10-10",
            endDate: "2024-11-11",
            isPercentage: false
        }
    ]

    return discountsDTO;
}


// export const getProductVariations = (productId) => {
//     const variationsDTO = [
//         {
//             id: 0,
//             name: "Variation A",
//             price: 10,
//             inventoryQuantity: 20,
//             productId: productId,
//             picture: "imageA.png",
//             discountId: null
//         },
//         {
//             id: 1,
//             name: "Variation B",
//             price: 12,
//             inventoryQuantity: 15,
//             productId: productId,
//             picture: "imageB.png",
//             discountId: 1
//         }
//     ];
//
//     return variationsDTO;
// };

