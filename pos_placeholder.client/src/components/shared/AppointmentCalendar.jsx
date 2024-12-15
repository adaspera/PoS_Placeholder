/* eslint-disable react/prop-types */
import DatePicker from "react-datepicker";
import "@/css/WorkTimeCalendar.css";
import "react-datepicker/dist/react-datepicker.css";
import { Col, Row, Button } from "reactstrap";
import { useState } from "react";

const AppointmentCalendar = ({ workTimes, service, appointments, onSelect }) => {
    const [selectedWorkDay, setSelectedWorkDay] = useState(null);
    const [selectedWorkTime, setSelectedWorkTime] = useState(null);

    const workDays = Array.from(
        new Set(workTimes.map((wt) => new Date(wt.day).toISOString().split("T")[0]))
    );

    const handleDateSelection = (date) => {
        const formattedDate = date.toISOString().split("T")[0];
        const workTime = workTimes.find(
            (wt) => new Date(wt.day).toISOString().split("T")[0] === formattedDate
        );

        setSelectedWorkDay(date);
        setSelectedWorkTime(workTime);
    };

    const generateAvailableTimes = () => {
        if (!selectedWorkTime || !service?.duration) return [];

        const availableTimes = [];
        const start = parseTimeToMinutes(selectedWorkTime.startTime);
        const end = parseTimeToMinutes(selectedWorkTime.endTime);
        const breakStart = selectedWorkTime.breakStart
            ? parseTimeToMinutes(selectedWorkTime.breakStart)
            : null;
        const breakEnd = selectedWorkTime.breakEnd
            ? parseTimeToMinutes(selectedWorkTime.breakEnd)
            : null;

        const duration = service.duration;

        for (let time = start; time + duration <= end; time += duration) {

            if (breakStart && time >= breakStart && time < breakEnd) continue;

            const timeSlot = formatMinutesToTime(time);

            const isReserved = appointments.some(
                (appt) =>
                    new Date(appt.timeReserved).toISOString().split("T")[0] ===
                    selectedWorkDay.toISOString().split("T")[0] &&
                    appt.timeReserved.includes(timeSlot)
            );

            if (!isReserved) {
                availableTimes.push(timeSlot);
            }
        }

        return availableTimes;
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
                    <h6>Available Times</h6>
                    <Row>
                        {generateAvailableTimes().map((timeSlot) => (
                            <Col key={timeSlot} xs="4" className="mb-2">
                                <Button
                                    color="primary"
                                    block
                                    onClick={() =>
                                        onSelect({
                                            timeReserved: `${selectedWorkDay
                                                .toISOString()
                                                .split("T")[0]}T${timeSlot}`,
                                        })
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
