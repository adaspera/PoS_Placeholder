import React from 'react';
import {Button, Col, Input, Label, Modal, ModalBody, ModalFooter, ModalHeader, Row, Form, FormGroup} from "reactstrap";
import {useEffect, useState} from "react";
import * as giftcardApi from "@/api/giftcardApi.jsx";
import {getCurrency} from "@/helpers/currencyUtils.jsx";
import toastNotify from "@/helpers/toastNotify.js";

function Giftcards() {
    const [allGiftcards, setAllGiftcards] = useState([]);
    const [isLoading, setIsLoading] = useState(true);
    const [editGiftcardFormOpenFor, setEditGiftcardFormOpenFor] = useState('');
    const [confirmDeleteGiftcardId, setConfirmDeleteGiftcardId] = useState(null);

    const [giftcardBalance, setGiftcardBalance] = useState('');
    const [editGiftcardBalance, setEditGiftcardBalance] = useState('');
    
    const fetchGiftcards = async () => {
        try {
            const fetchedGiftcards = await giftcardApi.getGiftcards();
            setAllGiftcards(fetchedGiftcards);
        } catch (error) {
            console.error("Error fetching all giftcards:", error);
        } finally {
            setIsLoading(false);
        }
    }

    useEffect(() => {
        fetchGiftcards();
    }, [])

    const handleCreateGiftcard = async () => {
        try {
            const createGiftcardDto = {
                BalanceAmount: Number(giftcardBalance),
            }

            const createdGiftcard = await giftcardApi.createGiftcard(createGiftcardDto);
            setAllGiftcards([...allGiftcards, createdGiftcard]);
            handleClearGiftcard();
            toastNotify("New gift card created!", "success");
        } catch (e) {
            toastNotify("Please provide balance field correctly.", "error");
        }
    }

    const handleUpdateGiftcard = async (giftcard) => {
        try {
            const updateGiftcardDto = {
                Id: giftcard.id,
                BalanceAmount: Number(editGiftcardBalance)
            }

            const updatedGiftcard = await giftcardApi.updateGiftcard(updateGiftcardDto);
            setAllGiftcards(allGiftcards.map((g) => (g.id === updatedGiftcard.id ? updatedGiftcard : g)));
            handleClearEditGiftcard();
            toastNotify("Gift card updated successfully!", "success");
        } catch (e) {
            toastNotify("Failed to update giftcard balance", "error");
        }
    }

    const handleRemoveGiftcardClick = (giftcardId) => {
        setConfirmDeleteGiftcardId(giftcardId);
    }

    const handleRemoveGiftcard = async () => {
        if (!confirmDeleteGiftcardId) return;
        try {
            await giftcardApi.deleteGiftcard(confirmDeleteGiftcardId);
            setAllGiftcards(prev => prev.filter(g => g.id !== confirmDeleteGiftcardId));
            setConfirmDeleteGiftcardId(null);
            toastNotify("Gift card deleted successfully!", "success");
        } catch (e) {
            console.log(e);
            toastNotify("Failed to delete giftcard", "error");
        }
    }

    const handleClearGiftcard = () => {
        setGiftcardBalance('');
    };

    const handleClearEditGiftcard = () => {
        setEditGiftcardBalance('');
        setEditGiftcardFormOpenFor('');
    };

    const editGiftcardForm = () => {
        const giftcard = allGiftcards.find(g => g.id === editGiftcardFormOpenFor);
        if (!giftcard) return null;
        return (
            <Modal isOpen={editGiftcardFormOpenFor !== ''} fade={true} size="lg" centered={true}>
                <ModalHeader>
                    Gift Card Id: {giftcard.id}
                </ModalHeader>
                <ModalBody>
                    <Form>
                        <FormGroup row>
                            <Label sm={3}>Gift card balance:</Label>
                            <Col sm={9}>
                                <Input
                                    value={editGiftcardBalance}
                                    type="number"
                                    onChange={(e) => setEditGiftcardBalance(e.target.value)}
                                />
                            </Col>
                        </FormGroup>
                    </Form>
                </ModalBody>
                <ModalFooter>
                    <Button color="success" className="me-3" onClick={() => handleUpdateGiftcard(giftcard)}>
                        Update
                    </Button>
                    <Button color="danger" onClick={handleClearEditGiftcard}>
                        Cancel
                    </Button>
                </ModalFooter>
            </Modal>
        );
    };

    const confirmDeleteModal = () => {
        if (!confirmDeleteGiftcardId) return null;
        return (
            <Modal isOpen={true} fade={true} size="md" centered={true}>
                <ModalHeader>
                    Confirm Deletion
                </ModalHeader>
                <ModalBody>
                    Are you sure you want to delete this giftcard?
                    Once deleted, you cannot refund orders that used it.
                </ModalBody>
                <ModalFooter>
                    <Button color="danger" className="me-3" onClick={handleRemoveGiftcard}>
                        Delete
                    </Button>
                    <Button color="secondary" onClick={() => setConfirmDeleteGiftcardId(null)}>
                        Cancel
                    </Button>
                </ModalFooter>
            </Modal>
        );
    };

    if (isLoading) {
        return <div>Loading...</div>;
    }

    return (
        <Row style={{height: "85vh"}}>
            <Col className="border rounded shadow-sm m-2 p-2 d-flex flex-column">
                <div className="justify-content-center border shadow-sm rounded p-2 mb-2">
                    <h4 className="d-flex justify-content-center">All Gift Cards</h4>
                </div>
                {allGiftcards.map((giftcard, index) => (
                    <div key={index} className="justify-content-center border rounded">
                        <Row className="p-2 align-items-center">
                            <Col lg={7}>
                                Giftcard Id: <span className="fw-bold">{giftcard.id}</span>
                            </Col>
                            <Col>
                                {getCurrency()} {giftcard.balance}
                            </Col>
                            <Col className="d-flex justify-content-start justify-content-sm-end mt-2 mt-md-0">
                                <Button
                                    color="secondary"
                                    outline
                                    onClick={() => setEditGiftcardFormOpenFor(giftcard.id)}
                                    className="me-3"
                                >
                                    <i className="bi-pencil"></i>
                                </Button>
                                <Button
                                    color="danger"
                                    onClick={() => handleRemoveGiftcardClick(giftcard.id)}
                                >
                                    <i className="bi-trash"></i>
                                </Button>
                            </Col>
                        </Row>
                    </div>
                ))}
            </Col>

            {editGiftcardForm()}
            {confirmDeleteModal()}

            <Col className="border rounded shadow-sm m-2 p-2">
                <div className="justify-content-center border shadow-sm rounded p-2 mb-2">
                    <h4 className="d-flex justify-content-center">Create new gift card</h4>
                </div>
                <div className="d-flex justify-content-center p-4">
                    <Label className="w-25 d-flex flex-column justify-content-center p-0 m-0">Gift card
                        balance {getCurrency()}: </Label>
                    <Input placeholder="Enter gift card balance amount"
                           type={"number"}
                           value={giftcardBalance}
                           onChange={(e) => setGiftcardBalance(e.target.value)}>
                    </Input>
                </div>
                <div className="d-flex justify-content-center mt-3">
                    <Button color="success" className="me-3" onClick={handleCreateGiftcard}>
                        Create
                    </Button>
                    <Button color="danger" onClick={handleClearGiftcard}>
                        Clear
                    </Button>
                </div>
            </Col>
        </Row>
    );
}

export default Giftcards;