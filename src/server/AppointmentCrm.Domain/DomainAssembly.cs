using System.Reflection;

namespace AppointmentCrm.Domain;

public static class DomainAssembly
{
    public static Assembly Assembly => typeof(DomainAssembly).Assembly;
}
