import {Button, Col, Form, FormGroup, Input, Label} from "reactstrap";
import {useState} from "react";

const ServiceForm = ({ onSubmit, prevService = emptyService }) => {
    const [service, setService] = useState(prevService);

    const handleInputChange = (e) => {
        const { id, value, type, checked } = e.target;

        setService((prevState) => ({
            ...prevState,
            [id]: type === "checkbox" ? checked : value,
        }));
    };

    const handleSubmit = (e) => {
        e.preventDefault();
        onSubmit(service);
    };

    return (
        <Form onSubmit={handleSubmit}>
            <FormGroup>
                <Label for="nameOfService">Name of service</Label>
                <Input
                    id="nameOfService"
                    placeholder="Enter the service name"
                    type="text"
                    value={service.nameOfService}
                    onChange={handleInputChange}
                    required
                />
            </FormGroup>
            <FormGroup>
                <Label for="serviceCharge">Service charge</Label>
                <Input
                    id="serviceCharge"
                    placeholder="Enter the service charge"
                    type="number"
                    value={service.serviceCharge}
                    onChange={handleInputChange}
                    required
                />
            </FormGroup>
            <FormGroup>
                <Label for="duration">Duration (minutes)</Label>
                <Input
                    id="duration"
                    placeholder="Enter the duration in minutes"
                    type="number"
                    value={service.duration}
                    onChange={handleInputChange}
                    required
                />
            </FormGroup>
            <FormGroup check>
                <Label check>
                    <Input
                        id="isPercentage"
                        type="checkbox"
                        checked={service.isPercentage}
                        onChange={handleInputChange}
                    />
                    &#32;
                    Is percentage based charge
                </Label>
            </FormGroup>
            <Col className="d-flex justify-content-center mt-5">
                <Button className="me-3" color="success" type="submit">
                    Submit
                </Button>
                <Button color="danger" onClick={() => setService(prevService)}>
                    Clear
                </Button>
            </Col>
        </Form>
    );
};

const emptyService = {
    nameOfService: "",
    serviceCharge: "",
    duration: "",
    isPercentage: false
}

export default ServiceForm;
