using System.Linq;
using ServiceStack.DataAccess;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.LogicFacade;
using ServiceStack.Service;
using ServiceStack.ServiceInterface;

namespace ServiceStack.Examples.ServiceInterface
{
	/// <summary>
	/// The service or 'Port' handler that will be used to execute the request.
	/// 
	/// The 'Port' attribute is used to link the 'service request' to the 'service implementation'
	/// </summary>
	[Port(typeof(StoreNewUser))]
	public class StoreNewUserHandler : IService
	{
		public object Execute(IOperationContext context)
		{
			var request = context.Request.Get<StoreNewUser>();


			//Get the persistence provider registered in the AppHost
			var manager = context.Application.Factory.Resolve<IPersistenceProviderManager>();
			var persistenceProvider = (IQueryablePersistenceProvider)manager.GetProvider();

			var existingUsers = persistenceProvider.Query<User>(user => user.UserName == request.UserName).ToList();

			if (existingUsers.Count > 0)
			{
				return new StoreNewUserResponse {
					ResponseStatus = new ResponseStatus { ErrorCode = "UserAlreadyExists" }
				};
			}

			var newUser = new User { UserName = request.UserName, Email = request.Email, Password = request.Password };

			persistenceProvider.Store(newUser);

			return new StoreNewUserResponse { UserId = newUser.Id };
		}
	}
}