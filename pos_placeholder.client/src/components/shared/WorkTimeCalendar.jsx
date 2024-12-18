import DatePicker from "react-datepicker";
import "@/css/WorkTimeCalendar.css";
import "react-datepicker/dist/react-datepicker.css";

const WorkTimeCalendar = ({ workTimes, onDateSelect }) => {
    const workDays = workTimes.map((wt) => new Date(wt.day).setHours(0, 0, 0, 0));

    return (
        <div>
            <DatePicker
                selected={null}
                onChange={(date) => onDateSelect(date)}
                highlightDates={workDays.map(date => new Date(date))}
                inline
            />
        </div>
    );
};

export default WorkTimeCalendar;
