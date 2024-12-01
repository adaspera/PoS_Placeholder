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
        localStorage.setItem("authToken", authToken);
    } catch (error) {
        console.error("Login failed:", error.message);
    }
}
