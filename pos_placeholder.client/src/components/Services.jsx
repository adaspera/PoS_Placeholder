import {
    Accordion, AccordionBody, AccordionHeader, AccordionItem,
    Button,
    Col, Modal, ModalBody, ModalFooter,
    Row
} from "reactstrap";
import {useEffect, useState} from "react";
import * as serviceApi from "@/api/serviceApi.jsx";
import ServiceForm from "@/components/shared/ServiceForm.jsx";
import AppointmentCalendar from "@/components/shared/AppointmentCalendar.jsx";
import * as businessApi from "@/api/businessApi.jsx";


const Services = () => {
    const [isLoading, setIsLoading] = useState(true);
    const [services, setServices] = useState([]);
    const [workTimes, setWorkTimes] = useState([]);
    const [appointments, setAppointments] = useState([]);
    const [employees, setEmployees] = useState([]);

    const [appointmentModalOpenFor, setAppointmentModalOpenFor] = useState('');
    const [open, setOpen] = useState('');

    const fetchStartupData = async () => {
        await fetchServices();
        await fetchEmployees();
        setIsLoading(false);
    }

    const fetchServices = async () => {
        const fetchedServices = await serviceApi.getAllServices();
        setServices(fetchedServices);
    }

    const fetchEmployees = async () => {
        const fetchedEmployees = await businessApi.getAllEmployees();
        setEmployees(fetchedEmployees);
    }

    useEffect( () => {
        fetchStartupData();
    },[]);


    const toggleAccordion = async (id) => {
        if (open === id) {
            setOpen(null);
        } else {
            setOpen(id);
        }
    };

    const openCreateAppointmentModal = async (service) => {
        setAppointmentModalOpenFor(service.id);
        const fetchedWorkTimes = await businessApi.getWorkTimes(service.userId);
        setWorkTimes(fetchedWorkTimes);
        const fetchedAppointments = await serviceApi.getServiceAppointmentsByUserId(service.userId);
        setAppointments(fetchedAppointments);
    }

    const removeService = async (id) => {
        await serviceApi.deleteService(id);
        setServices(services.filter(service => service.id !== id));
    }

    const handleCreateService = async (service) => {
        const createdService = await serviceApi.createService(service);
        setServices([...services, createdService]);
    }

    const handleUpdateService = async (service) => {
        service = {...service, id: open}
        const updatedService = await serviceApi.updateService(service);
        setServices(
            services.map((s) => (s.id === updatedService.id ? updatedService : s))
        );
        setOpen('');
    }

    const handleCreateAppointment = async (appointment) => {
        console.log(appointment);
        const newAppointment = await serviceApi.createServiceAppointment(appointment);
        setAppointments([...appointments, newAppointment]);
        setAppointmentModalOpenFor('');
    }

    const handleCancelAppointment = async (id) => {
        await serviceApi.deleteServiceAppointment(id);
        setAppointments(appointments.filter(appointment => appointment.id !== id))
    }

    const apointmentModal = (
        <Modal isOpen={!!appointmentModalOpenFor} fade={false} size="lg" centered>
            <ModalBody>
                <h5 className="mb-3">
                    {services.find((e) => e.id === appointmentModalOpenFor)?.name}
                </h5>
                <AppointmentCalendar
                    workTimes={workTimes}
                    service={services.find((e) => e.id === appointmentModalOpenFor)}
                    appointments={appointments}
                    onSelect={handleCreateAppointment}
                    onDelete={handleCancelAppointment}
                />
            </ModalBody>
            <ModalFooter>
                <Button color="secondary" onClick={() => setAppointmentModalOpenFor('')}>
                    Cancel
                </Button>
            </ModalFooter>
        </Modal>
    );

    if (isLoading) {
        return <div>Loading...</div>;
    }

    return (
        <Row style={{ height: "85vh" }}>
            <Col className="border rounded shadow-sm m-2 p-0 d-flex flex-column">
                <h4 className="p-2 d-flex justify-content-center">All services</h4>
                <Accordion open={open} toggle={toggleAccordion}>
                    {services.map((service) => (
                        <AccordionItem key={service.id}>
                            <AccordionHeader targetId={service.id.toString()}>
                                <div className="d-flex justify-content-between w-100 me-3">
                                    {service.name}
                                    <div>
                                        <Button
                                            color="danger"
                                            onClick={() => removeService(service.id)}
                                            className="ms-auto"
                                        >
                                            <i className="bi-trash"></i>
                                        </Button>
                                    </div>
                                </div>
                            </AccordionHeader>
                            <AccordionBody accordionId={service.id.toString()}>
                                <div className="mb-3">
                                    <Button
                                        color="secondary"
                                        onClick={() => openCreateAppointmentModal(service)}
                                        className="ms-auto"
                                        outline
                                    >
                                        Create an appointment <i className="bi-calendar"></i>
                                    </Button>
                                </div>
                                <ServiceForm onSubmit={handleUpdateService} employees={employees} prevService={service}/>
                            </AccordionBody>
                        </AccordionItem>
                    ))}
                </Accordion>
                {apointmentModal}
            </Col>
            <Col className="border rounded shadow-sm m-2 p-4">
                <h4 className="p-2 d-flex justify-content-center">Create new service</h4>
                <ServiceForm employees={employees} onSubmit={handleCreateService}/>
            </Col>
        </Row>
    );
}

export default Services;