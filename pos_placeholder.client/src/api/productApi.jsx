import { apiService } from "./ApiService";

export const getProducts = async () => {
    try {
        return await apiService.get("/api/products");
    } catch (e) {
        console.error("Error fetching products:", e);
    }
};

export const addProduct = async (product) => {
    try {
        return await apiService.post("/api/products", product);
    }  catch (e) {
        console.log("Error creating product:", e);
    }
};

export const deleteProduct = async (id) => {
    try {
        await apiService.delete(`/api/products/${id}`);
    } catch (e) {
        console.error("Error deleting product:", e);
    }
};

// export const getProductVariations = async (productId) => {
//     return await apiService.get(`/api/products/${productId}/variations`);
// };


export const getProductVariations = (productId) => {
    const variationsDTO = [
        {
            id: 0,
            name: "Variation A",
            price: 10,
            inventoryQuantity: 20,
            productId: productId,
            picture: "imageA.png",
            discountId: null
        },
        {
            id: 1,
            name: "Variation B",
            price: 12,
            inventoryQuantity: 15,
            productId: productId,
            picture: "imageB.png",
            discountId: 1
        }
    ];

    return variationsDTO;
};

// export const getProducts = () => {
//     // const ProductsDTO = fetch();
//     const productsDTO = [
//         { id: 1, name: "Item 1", itemGroup: "Group1" },
//         { id: 2, name: "Item 2", itemGroup: "Group1" },
//         { id: 3, name: "Item 3", itemGroup: "Group2" },
//         { id: 4, name: "Item 4", itemGroup: "Group2" },
//         { id: 5, name: "Item 5", itemGroup: "Group2" }
//     ];
//
//     return productsDTO;
// }

