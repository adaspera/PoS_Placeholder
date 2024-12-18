/* eslint-disable react/prop-types */
import DatePicker from "react-datepicker";
import "@/css/WorkTimeCalendar.css";
import "react-datepicker/dist/react-datepicker.css";
import {Col, Row, Button, Form, FormGroup, Label, Input} from "reactstrap";
import { useState } from "react";

const AppointmentCalendar = ({ workTimes, service, appointments, onSelect, onDelete }) => {
    const [selectedWorkDay, setSelectedWorkDay] = useState(null);
    const [selectedWorkTime, setSelectedWorkTime] = useState(null);

    const [customerName, setCustomerName] = useState("");
    const [customerPhone, setCustomerPhone] = useState("");

    //sitas agidys mane pribaigs AAAAAAAAAAAAAAAAAAA
    const formatDateLocal = (date) => {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, "0");
        const day = String(date.getDate()).padStart(2, "0");
        return `${year}-${month}-${day}`;
    };

    const workDays = Array.from(
        new Set(workTimes.map((wt) => formatDateLocal(new Date(wt.day))))
    );

    const handleDateSelection = (date) => {
        const formattedDate = formatDateLocal(date);
        const workTime = workTimes.find(
            (wt) => formatDateLocal(new Date(wt.day)) === formattedDate
        );

        setSelectedWorkDay(date);
        setSelectedWorkTime(workTime);
    };

    const generateTimes = () => {
        if (!selectedWorkTime || !service?.duration) return [];

        const availableTimes = [];
        const unavailableTimes = [];

        const start = parseTimeToMinutes(selectedWorkTime.startTime);
        const end = parseTimeToMinutes(selectedWorkTime.endTime);
        const breakStart = selectedWorkTime.breakStart
            ? parseTimeToMinutes(selectedWorkTime.breakStart)
            : null;
        const breakEnd = selectedWorkTime.breakEnd
            ? parseTimeToMinutes(selectedWorkTime.breakEnd)
            : null;

        const duration = service.duration;
        const selectedDateStr = formatDateLocal(selectedWorkDay);

        for (let time = start; time + duration <= end; time += duration) {
            if (breakStart && time >= breakStart && time < breakEnd) continue;

            const timeSlot = formatMinutesToTime(time);

            const matchedAppointment = appointments.find((appt) => {
                const apptDate = formatDateLocal(new Date(appt.timeReserved));
                return apptDate === selectedDateStr && appt.timeReserved.includes(timeSlot);
            });

            if (!matchedAppointment) {
                availableTimes.push(timeSlot);
            } else {
                unavailableTimes.push({ timeSlot, appointmentId: matchedAppointment.id });
            }
        }

        return { availableTimes, unavailableTimes };
    };

    const parseTimeToMinutes = (time) => {
        const [hours, minutes] = time.split(":").map(Number);
        return hours * 60 + minutes;
    };

    const formatMinutesToTime = (minutes) => {
        const hours = Math.floor(minutes / 60).toString().padStart(2, "0");
        const mins = (minutes % 60).toString().padStart(2, "0");
        return `${hours}:${mins}`;
    };

    const handleTimeSelection = (timeSlot) => {
        if (customerPhone === "" || customerName === "") {
            alert("Please enter a customer phone and name");
        } else {
            onSelect({
                timeReserved: `${formatDateLocal(selectedWorkDay)}T${timeSlot}`,
                customerName: customerName,
                customerPhone: customerPhone,
                serviceId: service.id
            })
        }
    }

    return (
        <Col>
            <div>
                <h5>Select a Work Day</h5>
                <DatePicker
                    selected={selectedWorkDay}
                    onChange={(date) => handleDateSelection(date)}
                    highlightDates={workDays.map((day) => new Date(day))}
                    inline
                />
            </div>
            {selectedWorkTime ? (
                <div className="mt-3">
                    <h6>
                        Customer information
                    </h6>
                    <Row>
                        <Form>
                            <FormGroup>
                                <Label for="customerName">Customer name</Label>
                                <Input id="customerName"
                                       type="text"
                                       value={customerName}
                                       onChange={(e) => setCustomerName(e.target.value)}
                                />
                            </FormGroup>
                            <FormGroup>
                                <Label for="customerPhone">Customer phone number</Label>
                                <Input id="customerName"
                                       type="text"
                                       value={customerPhone}
                                       onChange={(e) => setCustomerPhone(e.target.value)}
                                />
                            </FormGroup>
                        </Form>
                    </Row>
                    <h6>Available Times</h6>
                    <Row>
                        {generateTimes().availableTimes?.map((timeSlot) => (
                            <Col key={timeSlot} xs="4" className="mb-2">
                                <Button
                                    color="primary"
                                    block
                                    onClick={() => handleTimeSelection(timeSlot)}
                                >
                                    {timeSlot}
                                </Button>
                            </Col>
                        ))}
                    </Row>
                    <h6>Unavailable Times</h6>
                    <Row>
                        {generateTimes().unavailableTimes?.map(({ timeSlot, appointmentId }) => (
                            <Col key={timeSlot} xs="4" className="mb-2">
                                <Button
                                    color="danger"
                                    block
                                    onClick={() =>
                                        onDelete(appointmentId)
                                    }
                                >
                                    {timeSlot}
                                </Button>
                            </Col>
                        ))}
                    </Row>
                </div>
            ) : (
                <div className="mt-3 text-center">
                    <h6>No work time available for the selected date.</h6>
                </div>
            )}
        </Col>
    );
};

export default AppointmentCalendar;
