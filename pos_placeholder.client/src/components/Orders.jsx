import {Button, Col, Input, Label, Modal, ModalBody, ModalFooter, ModalHeader, Row, Form, FormGroup} from "reactstrap";
import {useEffect, useState} from "react";
import {getCurrency} from "@/helpers/currencyUtils.jsx";

const Orders = () => {


    return (
        <Row style={{height: "85vh"}}>
            <Col className="border rounded shadow-sm m-2 p-2 d-flex flex-column" lg={4}>
                Order list
            </Col>
            <Col className="border rounded shadow-sm m-2 p-2">
                Specific fetched order ui (when view receipt button is pressed)
            </Col>
        </Row>
    );
};

export default Orders;