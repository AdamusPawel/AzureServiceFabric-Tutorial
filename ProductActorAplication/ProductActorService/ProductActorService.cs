using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using ProductActorService.Interfaces;
using Contracts;
using System.IO;

namespace ProductActorService
{
    [StatePersistence(StatePersistence.Persisted)]
    internal class ProductActorService : Actor, IProductActorService, IRemindable
    {
        private string ProductStateName = "ProductState";

        private IActorTimer _actorTimer;

        public ProductActorService(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        public async Task AddProductAsync(Product product, CancellationToken cancellationToken)
        {
            await this.StateManager.AddOrUpdateStateAsync(ProductStateName, product, updateValueFactory: (key, value) => product, cancellationToken);

            await this.StateManager.SaveStateAsync(cancellationToken);
        }

        public async Task<Product> GetProductAsync(CancellationToken cancellationToken)
        {
            var product = await this.StateManager.GetStateAsync<Product>(ProductStateName, cancellationToken);

            return product;
        }

        protected override Task OnPostActorMethodAsync(ActorMethodContext actorMethodContext)
        {
            ActorEventSource.Current.ActorMessage(actor: this, message: $"{actorMethodContext.MethodName} has finished.");

            return base.OnPostActorMethodAsync(actorMethodContext);
        }

        protected override Task OnPreActorMethodAsync(ActorMethodContext actorMethodContext)
        {
            ActorEventSource.Current.ActorMessage(actor: this, message: $"{actorMethodContext.MethodName} will start soon.");

            return base.OnPreActorMethodAsync(actorMethodContext);
        }

        protected override Task OnDeactivateAsync()
        {
            if (_actorTimer != null)
            {
                UnregisterTimer(_actorTimer); // safe way to unregister timer when actor is deactivated
            }

            ActorEventSource.Current.ActorMessage(actor: this, message: "Actor deactivated.");

            return base.OnDeactivateAsync();
        }

        protected override Task OnActivateAsync()
        {
            var dbPort = Environment.GetEnvironmentVariable("DbPort");

            
            var dbConfig = this.ActorService.Context
                .CodePackageActivationContext
                .GetConfigurationPackageObject("Config")
                .Settings
                .Sections["Database"]
                .Parameters["DbConfig"]
                .Value;
            
            
            var dataPackage = this.ActorService.Context.CodePackageActivationContext.GetDataPackageObject("Data");

            var dataPath = Path.Combine(dataPackage.Path, "test.csv");

            var contents = File.ReadAllText(dataPath);
            

            this.RegisterReminderAsync("TaskReminder", null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15)); // register reminder

            ActorEventSource.Current.ActorMessage(actor: this, message: "Actor activated.");

            return this.StateManager.TryAddStateAsync("count", value: 0);
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan duetime, TimeSpan period) // receive reminder
        {
            if (reminderName == "TaskReminder")
            {
                ActorEventSource.Current.ActorMessage(actor: this, message: $"Reminder is doing work");
            }
        }
    }
}
