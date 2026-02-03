using R.O.S.C.H.API.DTO;

namespace R.O.S.C.H.API.Service.Interface;

public interface IControlService
{
    Task ControlLogProcess(ControlRequest request, string deviceAlias = "");
}