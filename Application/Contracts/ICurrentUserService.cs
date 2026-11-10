namespace Application.Contracts
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }

        /// <summary>Si el usuario tiene el permiso (Administrador no se considera incluido).</summary>
        bool TienePermiso(Application.Security.AppPermission permiso);
    }
}
