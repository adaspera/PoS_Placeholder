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
        await businessApi.deleteEmployee(id)
        setEmployees(employees.filter(employee => employee.id !== id));
    }

    const handleRegisterEmployee = async (credentials) => {
        try {
            const response = await businessApi.registerEmployee(credentials);
            if (response.isSuccess){
                setEmployees([...employees, response.data]);
            }
            else {
                alert("Registration failed.");
            }
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
            phoneNumber: employee.phoneNumber,
            email: employee.email,
            status: employee.status
        }
    }

    const handleCloseModal = async () => {
        setSelectedWorkTime(null);
        setScheduleModalOpenFor(null);
    };

    //kad nesitrainiot su laiko juostom
    function toUtcMidnightIsoDate(localDate) {
        localDate.setHours(12);
        const isoDate = localDate.toISOString().split('T')[0] + 'T00:00:00Z';
        return isoDate;
    }

    const handleWorkTimeDateSelection = (date) => {
        const selected = workTimes.find(wt => new Date(wt.day).toDateString() === date.toDateString());
        const isoDate = toUtcMidnightIsoDate(date);

        if (selected) {
            setSelectedWorkTime({
                ...selected,
                day: isoDate,
                breakStart: selected.breakStart || "", // It doesn't clear it otherwise for some reason
                breakEnd: selected.breakEnd || ""
            });
        } else {
            setSelectedWorkTime({
                userId: scheduleModalOpenFor,
                day: isoDate,
                startTime: "",
                endTime: "",
                breakStart: "",
                breakEnd: "",
            });
        }
    };

    const addWorkTime = async () => {
        if (selectedWorkTime.endTime === "" || selectedWorkTime.startTime === ""
            || selectedWorkTime.breakStart === "" || selectedWorkTime.breakEnd === "") {
            alert("Please select all times");
        } else {
            const existingWorkTimeIndex = workTimes.findIndex(
                (wt) => new Date(wt.day).toDateString() === new Date(selectedWorkTime.day).toDateString()
            );

            if (existingWorkTimeIndex !== -1) {
                console.log(selectedWorkTime);
                const newWorkTime = await businessApi.updateWorkTime(selectedWorkTime);
                console.log(newWorkTime);
                const updatedWorkTimes = [...workTimes];
                updatedWorkTimes[existingWorkTimeIndex] = newWorkTime;
                setWorkTimes(updatedWorkTimes);
            } else {
                console.log(selectedWorkTime);
                const newWorkTime = await businessApi.createWorkTime(selectedWorkTime);
                console.log(newWorkTime);
                setWorkTimes([...workTimes, newWorkTime]);
            }
            setSelectedWorkTime(null);
        }
    };

    const removeWorkTime = async () => {
        await businessApi.deleteWorkTime(selectedWorkTime);
        setWorkTimes(workTimes.filter(wt => wt.id !== selectedWorkTime.id));
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
                            <Label for="breakEnd">Break end</Label>
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
                    <Button color="secondary" onClick={() => removeWorkTime()}>Remove day</Button>
                </div>
            </ModalBody>
            <ModalFooter>
                <Button color="secondary" onClick={() => handleCloseModal()}>
                    Close
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
                                    formId={employee.id}
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