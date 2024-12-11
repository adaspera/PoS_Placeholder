import { useState } from "react";
import { login } from "@/api/AuthService.jsx";
import {Alert, Button, Col, Container, Form, FormGroup, Input, Label, Row} from "reactstrap";

//"owner@gmail.com","Owner123*"
const Login = ({ onLogin }) => {
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState(null);
    const [success, setSuccess] = useState(false);

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

    const autofill = () => {
        setEmail("owner@gmail.com");
        setPassword("Owner123*");
    }

    return (
        <Container className="m-5">
            <Row className="justify-content-center">
                <Col md={6} className="border rounded shadow h-auto p-5">
                    <h2 className="text-center">Login</h2>
                    {error && <Alert color="danger">{error}</Alert>}
                    {success && <Alert color="success">Login successful!</Alert>}
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
                </Col>
            </Row>
        </Container>
    );
};

export default Login;
