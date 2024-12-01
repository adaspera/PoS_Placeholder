const API_BASE_URL = "https://localhost:7228";

class ApiService {
    constructor(baseURL) {
        this.baseURL = baseURL;
    }

    async request(endpoint, options = {}) {
        const url = `${this.baseURL}${endpoint}`;

        const token = localStorage.getItem("authToken");

        const headers = {
            "Content-Type": "application/json",
            ...options.headers,
        };

        if (token) {
            headers["Authorization"] = `Bearer ${token}`;
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

            return await response.json();
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
            body: JSON.stringify(body),
        });
    }

    put(endpoint, body) {
        return this.request(endpoint, {
            method: "PUT",
            body: JSON.stringify(body),
        });
    }

    delete(endpoint) {
        return this.request(endpoint, {
            method: "DELETE",
        });
    }
}

export const apiService = new ApiService(API_BASE_URL);