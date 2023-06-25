using todolist.Models;

namespace todolist.UtilityService
{
    public interface IEmailService
    {

        void SendEmail(EmailModel emailmodel) ;
    }
}
