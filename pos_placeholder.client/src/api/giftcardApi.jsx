import { apiService } from "./ApiService";

export const getGiftcards = async () => {
    try {
        return await apiService.get("/api/giftcards")
    } catch (e) {
        console.log(e);
    }
}

export const createGiftcard = async (createGiftcardDto) => {
    try {
        return await apiService.post("/api/giftcards", createGiftcardDto);
    } catch (e) {
        console.log(e);
        throw e;
    }
}

export const updateGiftcard = async (updateGiftcardDto) => {
    try {
        return await apiService.put("/api/giftcards", updateGiftcardDto);
    } catch (e) {
        console.log(e);
        throw e;
    }
}

export const deleteGiftcard = async (giftcardId) => {
    try {
        return await apiService.delete(`/api/giftcards/${giftcardId}`);
    } catch (e) {
        console.log(e);
        throw e;
    }
}