import {Button, Form, FormGroup, Input, Label, Row} from "reactstrap";
import {useState} from "react";

const EmployeeForm = ({ onSubmit, credentials }) => {
    const [newCredentials, setNewCredentials] = useState(credentials);

    const handleInputChange = (e) => {
        const { id, value } = e.target;
        setNewCredentials((prevState) => ({
            ...prevState,
            [id]: value,
        }));
    };

    return (
        <Form onSubmit={(e) => {
            e.preventDefault();
            onSubmit(newCredentials);
        }}>
            {Object.entries(newCredentials)
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
                            onChange={handleInputChange}
                            placeholder={`Enter your ${key
                                .replace(/([A-Z])/g, " $1")
                                .toLowerCase()}`}
                            type={key.includes("password") || key.includes("confirmPassword") ? "password" : "text"}
                            required
                        />
                    </FormGroup>
                ))}
            <Row className="d-flex justify-content-center align-items-center">
                <Button color="success" className="me-3 w-25" type="submit">
                    Create
                </Button>
                <Button color="danger" className="w-25" onClick={() => setNewCredentials(credentials)}>
                    Clear
                </Button>
            </Row>
        </Form>
    );
}

export default EmployeeForm;