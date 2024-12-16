import { apiService } from "./ApiService";

export const getAllEmployees = async () => {
    //return testUsers;
    try {
        return await apiService.get("/api/business/employees");
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

export const updateEmployee = async (employee) => {
    try {
        return await apiService.put("/api/business/employees", employee);
    } catch (e) {
        console.error("Error updating employee:", e);
    }
};

export const deleteEmployee = async (id) => {
    try {
        return await apiService.delete(`/api/business/employees/${id}`);
    } catch (e) {
        console.error("Error deleting employee:", e);
    }
};

export const getWorkTimes = async (id) => {
    //return userWorkTimes;
    try {
        return await apiService.get(`/api/business/users/${id}/schedules`);
    } catch (e) {
        console.error("Error getting work times:", e);
    }
};

export const createWorkTime = async (workTime) => {
    try {
        return await apiService.post(`/api/business/users/${workTime.userId}/schedule`, workTime);
    } catch (e) {
        console.error("Error creating work time:", e);
    }
};

export const updateWorkTime = async (workTime) => {
    try {
        return await apiService.put(`/api/business/users/${workTime.userId}/schedule/${workTime.id}`, workTime);
    } catch (e) {
        console.error("Error updating work time:", e);
    }
};

export const deleteWorkTime = async (workTime) => {
    try {
        return await apiService.delete(`/api/business/users/${workTime.userId}/schedule/${workTime.id}`);
    } catch (e) {
        console.error("Error getting work times:", e);
    }
};

const testUsers = [
    {
        id: 1,
        firstName: "John",
        lastName: "Doe",
        phone: "123-456-7890",
        email: "john.doe@example.com",
        role: "Admin",
        passwordHash: "hashedPassword1",
        status: "Available",
        breakStart: "2024-12-14T10:00:00",
        breakEnd: "2024-12-14T10:30:00",
        businessId: 101,
    },
    {
        id: 2,
        firstName: "Jane",
        lastName: "Smith",
        phone: "987-654-3210",
        email: "jane.smith@example.com",
        role: "Staff",
        passwordHash: "hashedPassword2",
        status: "On Break",
        breakStart: "2024-12-14T12:00:00",
        breakEnd: "2024-12-14T12:15:00",
        businessId: 101,
    },
    {
        id: 3,
        firstName: "Alice",
        lastName: "Brown",
        phone: "555-555-5555",
        email: "alice.brown@example.com",
        role: "Manager",
        passwordHash: "hashedPassword3",
        status: "Unavailable",
        breakStart: null,
        breakEnd: null,
        businessId: 102,
    },
    {
        id: 4,
        firstName: "Bob",
        lastName: "Johnson",
        phone: "444-444-4444",
        email: "bob.johnson@example.com",
        role: "Staff",
        passwordHash: "hashedPassword4",
        status: "Available",
        breakStart: "2024-12-14T15:00:00",
        breakEnd: "2024-12-14T15:20:00",
        businessId: 103,
    },
];

const userWorkTimes = [
    {
        id: 1,
        day: "2024-12-01T00:00:00Z",
        startTime: "08:00:00",
        endTime: "16:00:00",
        userId: 1,
        breakStart: "12:00:00",
        breakEnd: "12:30:00"
    },
    {
        id: 3,
        day: "2024-12-02T00:00:00Z",
        startTime: "10:00:00",
        endTime: "18:00:00",
        userId: 1,
        breakStart: "14:00:00",
        breakEnd: "14:30:00"
    },
    {
        id: 4,
        day: "2024-12-03T00:00:00Z",
        startTime: "07:00:00",
        endTime: "15:00:00",
        userId: 1,
        breakStart: null,
        breakEnd: null
    }
];
