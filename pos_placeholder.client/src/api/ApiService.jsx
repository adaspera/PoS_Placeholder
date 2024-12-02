const API_BASE_URL = "https://localhost:7228";

class ApiService {
    constructor(baseURL) {
        this.baseURL = baseURL;
    }

    async request(endpoint, options = {}) {
        const url = `${this.baseURL}${endpoint}`;

        const token = localStorage.getItem("authToken");

        const headers = {
            ...options.headers,
        };

        if (token) {
            headers["Authorization"] = `Bearer ${token}`;
        }

        if (!(options.body instanceof FormData)) {
            headers["Content-Type"] = "application/json";
        }

        try {
            const response = await fetch(url, {
                headers,
                ...options,
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || "An error occurred");
            }

            const contentType = response.headers.get("Content-Type");
            if (contentType && contentType.includes("application/json")) {
                return await response.json();
            }

            return response;
        } catch (error) {
            console.error("API Error:", error);
            throw error;
        }
    }

    get(endpoint, params = {}) {
        const queryString = new URLSearchParams(params).toString();
        return this.request(`${endpoint}?${queryString}`);
    }

    post(endpoint, body) {
        return this.request(endpoint, {
            method: "POST",
            body: body instanceof FormData ? body : JSON.stringify(body),
        });
    }

    put(endpoint, body) {
        return this.request(endpoint, {
            method: "PUT",
            body: body instanceof FormData ? body : JSON.stringify(body),
        });
    }

    delete(endpoint) {
        return this.request(endpoint, {
            method: "DELETE",
        });
    }
}

export const apiService = new ApiService(API_BASE_URL);
