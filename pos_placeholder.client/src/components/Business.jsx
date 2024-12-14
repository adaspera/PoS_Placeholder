import {
    Accordion,
    AccordionBody,
    AccordionHeader,
    AccordionItem,
    Button,
    Col, Form,
    FormGroup,
    Input,
    Label,
    Row
} from "reactstrap";
import {useEffect, useState} from "react";
import * as businessApi from "@/api/businessApi.jsx";
import * as ProductApi from "@/api/productApi.jsx";

const Business = () => {
    const [isLoading, setIsLoading] = useState(true);

    const [employees, setEmployees] = useState([]);
    const [workTimes, setWorkTimes] = useState([]);
    const [registerCredentials, setRegisterCredentials] = useState({
        firstName: "",
        lastName: "",
        phoneNumber: "",
        email: "",
        password: "",
        confirmPassword: "",
    });

    const [isAddWorkTimeFormOpen, setIsAddWorkTimeFormOpen] = useState(false);
    const [editEmployeeFormOpenFor, setEditEmployeeFormOpenFor] = useState('');

    const [open, setOpen] = useState(null);


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
            setIsAddWorkTimeFormOpen(false);
            await fetchWorkTimes(id);
        }
    };

    const removeEmployee = async (id) => {
        await businessApi.deleteEmployee()
        setEmployees(employees.filter(employee => employee.id !== id));
    }

    if (isLoading) {
        return <div>Loading...</div>;
    }

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

                            </AccordionBody>
                        </AccordionItem>
                    ))}
                </Accordion>
            </Col>
            <Col className="border rounded shadow-sm m-2 p-4">
                <h4 className="p-2 d-flex justify-content-center">Register new employee</h4>
                <Form>
                    {Object.entries(registerCredentials)
                        .map(([key, value]) => (
                            <FormGroup key={key}>
                                <Label for={key}>
                                    {key
                                        .replace(/([A-Z])/g, " $1")
                                        .replace(/^./, (str) => str.toUpperCase())}
                                </Label>
                                <Input
                                    id={key}
                                    value={value}
                                    onChange={handleRegisterInputChange}
                                    placeholder={`Enter your ${key
                                        .replace(/([A-Z])/g, " $1")
                                        .toLowerCase()}`}
                                    type={key.includes("password") || key.includes("confirmPassword") ? "password" : "text"}
                                    required
                                />
                            </FormGroup>
                        ))}
                </Form>
                <Button color="success" className="me-3" onClick={addProduct}>
                    Create
                </Button>
                <Button color="danger" onClick={handleClearProduct}>
                    Clear
                </Button>
            </Col>
        </Row>
    );
}

export default Business;