import {apiService} from "@/api/ApiService.jsx";

export const getAllServices = async () => {
    //return services;
    try {
        return await apiService.get("/api/services/all");
    } catch (e) {
        console.error("Error fetching services:", e);
    }
}

export const createService = async (service) => {
    try {
        return await apiService.post("/api/services", service);
    } catch (e) {
        console.error("Error creating service:", e);
    }
}

export const updateService = async (service) => {
    try {
        return await apiService.put(`/api/services/${service.id}`, service);
    } catch (e) {
        console.error("Error updating service:", e);
    }
}

export const deleteService = async (id) => {
    try {
        return await apiService.delete(`/api/services/${id}`);
    } catch (e) {
        console.error("Error deleting service:", e);
    }
}

export const getServiceAppointmentsByUserId = async (id) => {
    //return appointments;
    try {
        return await apiService.get(`/api/appointments/user/${id}`);
    } catch (e) {
        console.error("Error fetching appointments:", e);
    }
}

export const createServiceAppointment = async (service) => {
    try {
        return await apiService.post("/api/appointments", service);
    } catch (e) {
        console.error("Error creating appointment:", e);
    }
}

export const deleteServiceAppointment = async (id) => {
    try {
        return await apiService.delete(`/api/appointments/${id}`);
    } catch (e) {
        console.error("Error deleting appointment:", e);
    }
}

const services = [
    {
        id: 1,
        name: "Haircut",
        serviceCharge: 20.00,
        duration: 30, // in minutes
        isPercentage: false,
        businessId: 101,
        userId: 1
    },
    {
        id: 2,
        name: "Manicure",
        serviceCharge: 15.50,
        duration: 45,
        isPercentage: false,
        businessId: 101,
        userId: 2
    },
    {
        id: 3,
        name: "Consultation",
        serviceCharge: 50.00,
        duration: 60,
        isPercentage: true, // Service charge is a percentage
        businessId: 102,
        userId: 3
    },
    {
        id: 4,
        name: "Massage",
        serviceCharge: 100.00,
        duration: 90,
        isPercentage: false,
        businessId: 103,
        userId: 4
    },
    {
        id: 5,
        name: "Pedicure",
        serviceCharge: 30.00,
        duration: 60,
        isPercentage: false,
        businessId: 104,
        userId: 5
    }
];

const appointments = [
    {
        id: 1,
        timeCreated: "2024-06-14T09:00:00Z",
        timeReserved: "2024-06-20T10:00:00Z",
        customerName: "John Doe",
        customerPhone: "123-456-7890",
        userId: 1,
        serviceId: 3
    },
    {
        id: 2,
        timeCreated: "2024-06-14T10:00:00Z",
        timeReserved: "2024-06-21T12:30:00Z",
        customerName: "Jane Smith",
        customerPhone: "987-654-3210",
        userId: 2,
        serviceId: 2
    },
    {
        id: 3,
        timeCreated: "2024-06-14T11:30:00Z",
        timeReserved: "2024-06-22T15:45:00Z",
        customerName: "Alice Brown",
        customerPhone: "555-555-1234",
        userId: 1,
        serviceId: 1
    },
    {
        id: 4,
        timeCreated: "2024-06-14T12:00:00Z",
        timeReserved: "2024-06-23T09:00:00Z",
        customerName: "Bob Johnson",
        customerPhone: "444-444-5678",
        userId: 3,
        serviceId: 4
    },
    {
        id: 5,
        timeCreated: "2024-06-14T12:45:00Z",
        timeReserved: "2024-06-24T14:15:00Z",
        customerName: "Eve Adams",
        customerPhone: "111-222-3333",
        userId: 4,
        serviceId: 5
    }
];