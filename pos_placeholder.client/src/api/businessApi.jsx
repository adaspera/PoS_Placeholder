import { apiService } from "./ApiService";

export const getAllEmployees = async () => {
    try {
        return await apiService.get("/api/user");
    } catch (e) {
        console.error("Error fetching employees:", e);
    }
};

export const getEmployee = async (id) => {
    try {
        return await apiService.get(`/api/user/${id}`);
    } catch (e) {
        console.error("Error fetching employee:", e);
    }
};

export const registerEmployee = async (employee) => {
    try {
        return await apiService.post("/api/auth/register-employee", employee);
    } catch (e) {
        console.error("Error registering employee:", e);
    }
};

export const deleteEmployee = async (id) => {

};

export const getWorkTimes = async (id) => {

};

export const createWorkTime = async (workTime) => {

};

export const updateWorkTime = async (workTime) => {

};

export const deleteWorkTime = async (workTime) => {

};


