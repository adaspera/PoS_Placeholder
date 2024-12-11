import {apiService} from "@/api/ApiService.jsx";

export async function login(email, password) {
    try {
        const loginRequestDto = {
            email: email,
            password: password,
        };

        const response = await apiService.post("/api/auth/login", loginRequestDto);

        console.log("Login successful:", response);

        const authToken = response.data.authToken;
        const currency = response.data.currency;

        localStorage.setItem("authToken", authToken);
        localStorage.setItem("currency", currency);

        return response;
    } catch (error) {
        console.error("Login failed:", error.message);
    }
}

export async function registerBusiness(credentials) {
    try {
        const response = await apiService.post("/api/auth/register-business", credentials);

        console.log("Register successful:", response);

        return response;
    } catch (error) {
        console.error("Register failed:", error.message);
    }
}
