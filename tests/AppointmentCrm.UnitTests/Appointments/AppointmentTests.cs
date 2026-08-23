using AppointmentCrm.Domain.Appointments;

namespace AppointmentCrm.UnitTests.Appointments;

public sealed class AppointmentTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 23, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_SnapshotsServiceAndBuildsScheduledHistory()
    {
        Appointment appointment = CreateAppointment();

        Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
        Assert.Equal(Now.AddDays(1).AddMinutes(30), appointment.EndsAtUtc);
        Assert.Equal("Consultation", appointment.ServiceName);
        Assert.Equal(30, appointment.ServiceDurationMinutes);
        Assert.Equal(250m, appointment.ServicePrice);
        Assert.Equal("TRY", appointment.ServiceCurrency);
        Assert.Equal(1, appointment.Revision);
        AppointmentStatusHistory history = Assert.Single(appointment.StatusHistory);
        Assert.Null(history.FromStatus);
        Assert.Equal(AppointmentStatus.Scheduled, history.ToStatus);
    }

    [Fact]
    public void StateMachine_AllowsOnlyDeclaredTransitionsAndProtectsRevision()
    {
        Appointment appointment = CreateAppointment();

        appointment.TransitionTo(
            AppointmentStatus.Confirmed,
            1,
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.AddHours(1));

        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);
        Assert.Equal(2, appointment.Revision);
        Assert.Throws<InvalidOperationException>(() => appointment.TransitionTo(
            AppointmentStatus.Cancelled,
            1,
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.AddHours(2)));
        Assert.Throws<InvalidOperationException>(() => appointment.TransitionTo(
            AppointmentStatus.Scheduled,
            2,
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.AddHours(2)));
    }

    [Fact]
    public void CompleteAndNoShow_AreRejectedBeforeStart()
    {
        Appointment appointment = CreateAppointment();
        appointment.TransitionTo(
            AppointmentStatus.Confirmed,
            1,
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.AddHours(1));

        Assert.Throws<InvalidOperationException>(() => appointment.TransitionTo(
            AppointmentStatus.Completed,
            2,
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.AddHours(2)));
    }

    [Fact]
    public void Reschedule_UsesSnapshotDurationAndTerminalAppointmentsCannotMove()
    {
        Appointment appointment = CreateAppointment();
        DateTimeOffset newStart = Now.AddDays(2);

        appointment.Reschedule(newStart, 1, Now.AddHours(1));

        Assert.Equal(newStart.AddMinutes(30), appointment.EndsAtUtc);
        Assert.Equal(2, appointment.Revision);
        appointment.TransitionTo(
            AppointmentStatus.Cancelled,
            2,
            "Customer request",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.AddHours(2));
        Assert.Throws<InvalidOperationException>(() => appointment.Reschedule(
            Now.AddDays(3),
            3,
            Now.AddHours(3)));
    }

    [Fact]
    public void Occupancy_UsesAcceptedHalfOpenConstraintStatusSet()
    {
        Assert.True(Appointment.OccupiesTime(AppointmentStatus.Scheduled));
        Assert.True(Appointment.OccupiesTime(AppointmentStatus.Confirmed));
        Assert.True(Appointment.OccupiesTime(AppointmentStatus.Completed));
        Assert.True(Appointment.OccupiesTime(AppointmentStatus.NoShow));
        Assert.False(Appointment.OccupiesTime(AppointmentStatus.Cancelled));
    }

    [Fact]
    public void StateMachine_DeclaresTheCompleteTransitionMatrix()
    {
        var expected = new HashSet<(AppointmentStatus From, AppointmentStatus To)>
        {
            (AppointmentStatus.Scheduled, AppointmentStatus.Confirmed),
            (AppointmentStatus.Scheduled, AppointmentStatus.Cancelled),
            (AppointmentStatus.Scheduled, AppointmentStatus.NoShow),
            (AppointmentStatus.Confirmed, AppointmentStatus.Completed),
            (AppointmentStatus.Confirmed, AppointmentStatus.Cancelled),
            (AppointmentStatus.Confirmed, AppointmentStatus.NoShow),
        };

        foreach (AppointmentStatus from in Enum.GetValues<AppointmentStatus>())
        {
            foreach (AppointmentStatus to in Enum.GetValues<AppointmentStatus>())
            {
                Assert.Equal(
                    expected.Contains((from, to)),
                    Appointment.CanTransition(from, to));
            }
        }
    }

    [Fact]
    public void CompleteAfterStart_TrimsReasonAndCreatesHistory()
    {
        Appointment appointment = CreateAppointment();
        appointment.TransitionTo(
            AppointmentStatus.Confirmed,
            1,
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.AddHours(1));

        AppointmentStatusHistory completed = appointment.TransitionTo(
            AppointmentStatus.Completed,
            2,
            "  Finished successfully  ",
            Guid.NewGuid(),
            Guid.NewGuid(),
            appointment.StartsAtUtc.AddMinutes(1));

        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
        Assert.Equal("Finished successfully", completed.Reason);
        Assert.Equal(3, appointment.Revision);
    }

    [Fact]
    public void Create_RejectsNonUtcBoundariesAndInvalidSnapshotEdges()
    {
        Assert.Throws<ArgumentException>(() => Appointment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 8, 24, 10, 0, 0, TimeSpan.FromHours(3)),
            "Consultation",
            30,
            250m,
            "TRY",
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => Appointment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.AddDays(1),
            "Consultation",
            31,
            250m,
            "TRY",
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => Appointment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.AddDays(1),
            "Consultation",
            30,
            250.001m,
            "TRY",
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now));
    }

    private static Appointment CreateAppointment() =>
        Appointment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.AddDays(1),
            "Consultation",
            30,
            250m,
            "TRY",
            "Initial visit",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now);
}
