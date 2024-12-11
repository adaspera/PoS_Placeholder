import { useState } from "react";
import {login, registerBusiness} from "@/api/AuthService.jsx";
import {Alert, Button, Col, Container, Form, FormGroup, Input, Label, Row} from "reactstrap";

//"owner@gmail.com","Owner123*"
const Login = ({ onLogin }) => {
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState(null);
    const [success, setSuccess] = useState(false);

    const [registerCredentials, setRegisterCredentials] = useState({
        businessName: "",
        businessPhone: "",
        businessEmail: "",
        businessStreet: "",
        businessCity: "",
        businessRegion: "EUR",
        businessCountry: "",
        firstName: "",
        lastName: "",
        phoneNumber: "",
        email: "",
        password: "",
    });
    // const [registerCredentials, setRegisterCredentials] = useState({
    //     businessName: "Abzora",
    //     businessPhone: "+37055555555",
    //     businessEmail: "testingg@gmail.com",
    //     businessStreet: "address 1",
    //     businessCity: "Kaunas",
    //     businessRegion: "EUR",
    //     businessCountry: "Lithuania",
    //     firstName: "Zmogus",
    //     lastName: "Voras",
    //     phoneNumber: "+37055555555",
    //     email: "testingg@gmail.com",
    //     password: "Slaptazodis1*",
    // });
    const [isWindowLogin, setIsWindowLogin] = useState(true);

    const handleRegisterInputChange = (e) => {
        const { id, value } = e.target;
        setRegisterCredentials((prevState) => ({
            ...prevState,
            [id]: value,
        }));
    };

    const handleLogin = async (e) => {
        e.preventDefault();
        setError(null);
        setSuccess(false);

        try {
            const response = await login(email, password);
            if (response.isSuccess) {
                onLogin();
                setSuccess(true);
            } else {
                setError("Login failed. Access denied");
            }
        } catch (err) {
            setError("Login failed. Please check your credentials.");
        }
    };

    const handleRegister = async (e) => {
        e.preventDefault();
        setError(null);
        setSuccess(false);

        try {
            const response = await registerBusiness(registerCredentials);
            if (response.isSuccess) {
                setIsWindowLogin(true);
                setSuccess(true);
            } else {
                setError("Login failed. Access denied");
            }
        } catch (err) {
            setError("Register failed. Please check your credentials.");
        }
    }

    const autofill = () => {
        setEmail("owner@gmail.com");
        setPassword("Owner123*");
    }

    const loginWindow =
        <Container className="m-5">
            <Row className="justify-content-center">
                <Col md={6} className="border rounded shadow h-auto p-5">
                    <h2 className="text-center">Login</h2>
                    {error && <Alert color="danger">{error}</Alert>}
                    {success && <Alert color="success">Login/Register successful!</Alert>}
                    <Form onSubmit={handleLogin}>
                        <FormGroup>
                            <Label for="email">Email</Label>
                            <Input
                                type="email"
                                id="email"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                                placeholder="Enter your email"
                                required
                            />
                        </FormGroup>
                        <FormGroup>
                            <Label for="password">Password</Label>
                            <Input
                                type="password"
                                id="password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                placeholder="Enter your password"
                                required
                            />
                        </FormGroup>
                        <Button color="primary" type="submit" block>
                            Login
                        </Button>
                        <Button color="secondary" onClick={() => autofill()} className="mt-1" block>
                            Autofill
                        </Button>
                    </Form>
                    <Button onClick={() => setIsWindowLogin(false)}
                            className="mt-4"
                            color="secondary"
                            outline>
                        Register
                    </Button>
                </Col>
            </Row>
        </Container>

    const registerWindow =
        <Container className="m-5">
            <Row className="justify-content-center">
                <Col md={6} className="border rounded shadow h-auto p-5">
                    <h2 className="text-center">Register a business</h2>
                    {error && <Alert color="danger">{error}</Alert>}
                    <Form onSubmit={handleRegister}>
                        {Object.entries(registerCredentials)
                            .filter(([key]) => key !== "businessRegion")
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
                                    type={key.includes("password") ? "password" : "text"}
                                    required
                                />
                            </FormGroup>
                        ))}
                        <FormGroup>
                            <Label for="businessRegion">Business Region (determines currency)</Label>
                            <Input
                                id="businessRegion"
                                type="select"
                                value={registerCredentials.businessRegion}
                                onChange={handleRegisterInputChange}
                                required
                            >
                                <option value="EUR">Eurozone</option>
                                <option value="USD">USA</option>
                            </Input>
                        </FormGroup>
                        <Button color="primary" type="submit" block>
                            Register
                        </Button>
                    </Form>
                    <Button onClick={() => setIsWindowLogin(true)}
                            className="mt-4"
                            color="secondary"
                            outline>
                        Login
                    </Button>
                </Col>
            </Row>
        </Container>

    return (
        isWindowLogin ?
        loginWindow :
        registerWindow
    );

};

export default Login;
