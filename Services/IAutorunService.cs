namespace APO.Services
{
    public interface IAutorunService
    {
        bool IsInAutorun();
        void SetAutorun(bool enable);
    }
}
