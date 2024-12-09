import {apiService} from "./ApiService";

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
    } catch (e) {
        console.log("Error creating product:", e);
    }
};

export const updateProduct = async (product) => {
    try {
        return await apiService.put("/api/products", product);
    } catch (e) {
        console.error("Error updating product:", e);
    }
};

export const deleteProduct = async (id) => {
    try {
        await apiService.delete(`/api/products/${id}`);
    } catch (e) {
        console.error("Error deleting product:", e);
    }
};

export const getProductVariations = async (productId) => {
    try {
        return await apiService.get(`/api/productVariations/${productId}`);
    } catch (e) {
        console.error("Error fetching productVariants:", e);
    }
};

export const addProductVariation = async (productVariation) => {
    try {
        return await apiService.post("/api/productVariations", productVariation);
    } catch (e) {
        console.error("Error creating productVariation:", e);
    }
};

export const updateProductVariation = async (productVariation) => {
    try {
        return await apiService.put("/api/productVariations", productVariation);
    } catch (e) {
        console.error("Error updating productVariation:", e);
    }
};

export const deleteProductVariation = async (id) => {
    try {
        await apiService.delete(`/api/productVariations/${id}`);
    } catch (e) {
        console.error("Error deleting productVariation:", e);
    }
};

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

