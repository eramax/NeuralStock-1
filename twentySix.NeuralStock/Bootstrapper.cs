namespace twentySix.NeuralStock
{
    using System;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.IO;
    using System.Reflection;
    using System.Windows;

    using Prism.Mef;

    using twentySix.NeuralStock.Core.Helpers;
    using twentySix.NeuralStock.Core.Services.Interfaces;

    public class Bootstrapper : MefBootstrapper
    {
        protected override void ConfigureAggregateCatalog()
        {
            var callingAssemblyLocation = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(Bootstrapper).Assembly));
            AggregateCatalog.Catalogs.Add(new DirectoryCatalog(callingAssemblyLocation ?? throw new InvalidOperationException(), "twentySix.*.dll"));

            base.ConfigureAggregateCatalog();
        }

        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();

            Container.ComposeExportedValue(AggregateCatalog);

            ApplicationHelper.StartUp(Container.GetExportedValue<ILoggingService>(), Container);
        }

        protected override DependencyObject CreateShell()
        {
            return Container.GetExportedValue<Main.Shell>();
        }

        protected override void InitializeShell()
        {
            base.InitializeShell();
            Application.Current.MainWindow = (Main.Shell)Shell;
            Application.Current.MainWindow?.Show();
        }
    }
}