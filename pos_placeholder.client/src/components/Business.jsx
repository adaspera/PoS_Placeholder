import {
    Accordion,
    AccordionBody,
    AccordionHeader,
    AccordionItem,
    Button,
    Col, Form,
    FormGroup,
    Input,
    Label, Modal, ModalBody, ModalFooter,
    Row
} from "reactstrap";
import {useEffect, useState} from "react";
import * as businessApi from "@/api/businessApi.jsx";
import EmployeeForm from "@/components/shared/EmployeeForm.jsx";
import WorkTimeCalendar from "@/components/shared/WorkTimeCalendar.jsx";

const Business = () => {
    const [isLoading, setIsLoading] = useState(true);

    const [employees, setEmployees] = useState([]);
    const [workTimes, setWorkTimes] = useState([]);
    const [selectedWorkTime, setSelectedWorkTime] = useState(null);

    const [open, setOpen] = useState(null);
    const [scheduleModalOpenFor, setScheduleModalOpenFor] = useState(null);


    const fetchEmployees = async () => {
        const fetchedEmployees = await businessApi.getAllEmployees();
        setEmployees(fetchedEmployees);
        setIsLoading(false);
    };

    const fetchWorkTimes = async (id) => {
        const fetchedWorkTimes = await businessApi.getWorkTimes(id);
        setWorkTimes(fetchedWorkTimes);
    };

    useEffect( () => {
        fetchEmployees();
    },[]);

    const toggleAccordion = async (id) => {
        if (open === id) {
            setOpen(null);
            setWorkTimes([])
        } else {
            setOpen(id);
            await fetchWorkTimes(id);
        }
    };

    const removeEmployee = async (id) => {
        await businessApi.deleteEmployee()
        setEmployees(employees.filter(employee => employee.id !== id));
    }

    const handleRegisterEmployee = async (credentials) => {
        try {
            const response = await businessApi.registerEmployee(credentials);
            if (!response.isSuccess)
                alert("Registration failed.");
        } catch (err) {
            alert("Register failed. Please check your credentials.");
            console.log(err);
        }
    }

    const handleUpdateEmployee = async (credentials) => {
        const updatedEnmployee = await businessApi.updateEmployee(credentials);
        setEmployees(
            employees.map((e) => (e.id === updatedEnmployee.id ? updatedEnmployee : e))
        );
    }

    const refactorForEditing = (employee) => {
        return {
            firstName: employee.firstName,
            lastName: employee.lastName,
            phone: employee.phone,
            email: employee.email,
            status: employee.status
        }
    }

    const handleUpdateWorkTimes = async () => {
        setSelectedWorkTime(null);
        await businessApi.updateWorkTimes(scheduleModalOpenFor, workTimes);
        setScheduleModalOpenFor(null);
    };

    const handleCancelUpdateWorkTimes = async () => {
        setSelectedWorkTime(null);
        // Nesmerkit tingiu galvot
        await businessApi.getWorkTimes(scheduleModalOpenFor);
        setScheduleModalOpenFor(null);
    }

    const handleWorkTimeDateSelection = (date) => {
        const selected = workTimes.find(wt => new Date(wt.day).toDateString() === date.toDateString());

        if (selected) {
            setSelectedWorkTime({
                ...selected,
                breakStart: selected.breakStart || "", // It doesn't clear it otherwise for some reason
                breakEnd: selected.breakEnd || ""
            });
        } else {
            setSelectedWorkTime({
                employeeId: scheduleModalOpenFor,
                day: date,
                startTime: "",
                endTime: "",
                breakStart: "",
                breakEnd: "",
            });
        }
    };

    const addWorkTime = () => {
        if (selectedWorkTime.endTime === "" || selectedWorkTime.startTime === "") {
            alert("Please select both work times");
        } else {
            const existingWorkTimeIndex = workTimes.findIndex(
                (wt) => new Date(wt.day).toDateString() === new Date(selectedWorkTime.day).toDateString()
            );

            if (existingWorkTimeIndex !== -1) {
                const updatedWorkTimes = [...workTimes];
                updatedWorkTimes[existingWorkTimeIndex] = selectedWorkTime;
                setWorkTimes(updatedWorkTimes);
            } else {
                setWorkTimes([...workTimes, selectedWorkTime]);
            }
            setSelectedWorkTime(null);
        }
    };

    const removeWorkTime = (day) => {
        setWorkTimes(workTimes.filter(wt => new Date(wt.day).toDateString() !== new Date(day).toDateString()));
        setSelectedWorkTime(null);
    };

    const scheduleModal = (
        <Modal isOpen={!!scheduleModalOpenFor} fade={false} size="lg" centered>
            <ModalBody>
                <h5 className="mb-3">
                    {employees.find((e) => e.id === scheduleModalOpenFor)?.firstName}
                    &#32;
                    {employees.find((e) => e.id === scheduleModalOpenFor)?.lastName}
                </h5>
                <WorkTimeCalendar
                    workTimes={workTimes}
                    onDateSelect={handleWorkTimeDateSelection}
                />
                {selectedWorkTime && (
                    <Form>
                        <FormGroup>
                            <Label for="startTime">Start time</Label>
                            <Input
                                id="startTime"
                                type="time"
                                value={selectedWorkTime.startTime}
                                onChange={(e) => setSelectedWorkTime((prev) => ({ ...prev, startTime: e.target.value }))}
                            />
                        </FormGroup>
                        <FormGroup>
                            <Label for="endTime">End time</Label>
                            <Input
                                id="endTime"
                                type="time"
                                value={selectedWorkTime.endTime}
                                onChange={(e) => setSelectedWorkTime((prev) => ({ ...prev, endTime: e.target.value }))}
                            />
                        </FormGroup>
                        <FormGroup>
                            <Label for="breakStart">Break start</Label>
                            <Input
                                id="breakStart"
                                type="time"
                                value={selectedWorkTime.breakStart}
                                onChange={(e) => setSelectedWorkTime((prev) => ({ ...prev, breakStart: e.target.value }))}
                            />
                        </FormGroup>
                        <FormGroup>
                            <Label for="breakEnd">Break start</Label>
                            <Input
                                id="breakEnd"
                                type="time"
                                value={selectedWorkTime.breakEnd}
                                onChange={(e) => setSelectedWorkTime((prev) => ({ ...prev, breakEnd: e.target.value }))}
                            />
                        </FormGroup>
                    </Form>
                )}
                <div className="mt-3">
                    <Button color="primary" className="me-3" onClick={addWorkTime}>Save day</Button>
                    <Button color="secondary" onClick={() => removeWorkTime(selectedWorkTime?.day)}>Remove day</Button>
                </div>
            </ModalBody>
            <ModalFooter>
                <Button color="secondary" onClick={() => handleCancelUpdateWorkTimes()}>
                    Cancel
                </Button>
                <Button color="primary" onClick={handleUpdateWorkTimes}>
                    Save
                </Button>
            </ModalFooter>
        </Modal>
    );


    if (isLoading)
        return <div>Loading...</div>;

    return (
        <Row style={{ height: "85vh" }}>
            <Col className="border rounded shadow-sm m-2 p-0 d-flex flex-column">
                <h4 className="p-2 d-flex justify-content-center">All employees</h4>
                <Accordion open={open} toggle={toggleAccordion}>
                    {employees.map((employee) => (
                        <AccordionItem key={employee.id}>
                            <AccordionHeader targetId={employee.id.toString()}>
                                <div className="d-flex justify-content-between w-100 me-3">
                                    {employee.firstName} {employee.lastName}
                                    <div>
                                        <Button
                                            color="danger"
                                            onClick={() => removeEmployee(employee.id)}
                                            className="ms-auto"
                                        >
                                            <i className="bi-trash"></i>
                                        </Button>
                                    </div>
                                </div>
                            </AccordionHeader>
                            <AccordionBody accordionId={employee.id.toString()}>
                                <div className="mb-3">
                                    <Button
                                        color="secondary"
                                        onClick={() => setScheduleModalOpenFor(employee.id)}
                                        className="ms-auto"
                                        outline
                                    >
                                        Change schedule <i className="bi-calendar"></i>
                                    </Button>
                                </div>
                                <EmployeeForm
                                    credentials={refactorForEditing(employee)}
                                    onSubmit={(updatedCredentials) => {
                                        handleUpdateEmployee({id: employee.id, ...updatedCredentials});
                                    }}
                                />
                            </AccordionBody>
                        </AccordionItem>
                    ))}
                </Accordion>
                {scheduleModal}
            </Col>
            <Col className="border rounded shadow-sm m-2 p-4">
                <h4 className="p-2 d-flex justify-content-center">Register new employee</h4>
                <EmployeeForm credentials={DefaultRegisterCredentials} onSubmit={handleRegisterEmployee}/>
            </Col>
        </Row>
    );
}

const DefaultRegisterCredentials = {
    firstName: "",
    lastName: "",
    phoneNumber: "",
    email: "",
    password: "",
    confirmPassword: "",
};


export default Business;