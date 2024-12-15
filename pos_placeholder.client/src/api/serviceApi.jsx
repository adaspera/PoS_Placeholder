export const getAllServices = async () => {
    return services;
}

export const createService = async (service) => {

}

export const updateService = async (service) => {

}

export const deleteService = async (id) => {

}

export const getServiceAppointments = async (id) => {
    return appointments;
}

export const createServiceAppointment = async (service) => {

}

export const deleteServiceAppointment = async (id) => {

}

const services = [
    {
        id: 1,
        nameOfService: "Haircut",
        serviceCharge: 20.00,
        duration: 30, // in minutes
        isPercentage: false,
        businessId: 101,
        userId: 1
    },
    {
        id: 2,
        nameOfService: "Manicure",
        serviceCharge: 15.50,
        duration: 45,
        isPercentage: false,
        businessId: 101,
        userId: 2
    },
    {
        id: 3,
        nameOfService: "Consultation",
        serviceCharge: 50.00,
        duration: 60,
        isPercentage: true, // Service charge is a percentage
        businessId: 102,
        userId: 3
    },
    {
        id: 4,
        nameOfService: "Massage",
        serviceCharge: 100.00,
        duration: 90,
        isPercentage: false,
        businessId: 103,
        userId: 4
    },
    {
        id: 5,
        nameOfService: "Pedicure",
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