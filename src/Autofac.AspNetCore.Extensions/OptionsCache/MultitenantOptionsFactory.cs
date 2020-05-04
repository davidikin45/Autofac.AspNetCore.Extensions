using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Autofac.AspNetCore.Extensions.OptionsCache
{
    public class MultitenantOptionsFactory<TOptions> : IOptionsFactory<TOptions> where TOptions : class, new()
    {
        private readonly IConfiguration _config;
        private readonly IEnumerable<IConfigureOptions<TOptions>> _setups;
        private readonly IEnumerable<IPostConfigureOptions<TOptions>> _postConfigures;
        private readonly IEnumerable<IValidateOptions<TOptions>> _validations;

        public MultitenantOptionsFactory(IConfiguration config, IEnumerable<IConfigureOptions<TOptions>> setups, IEnumerable<IPostConfigureOptions<TOptions>> postConfigures) : this(config, setups, postConfigures, validations: null)
        { }

        public MultitenantOptionsFactory(IConfiguration config, IEnumerable<IConfigureOptions<TOptions>> setups, IEnumerable<IPostConfigureOptions<TOptions>> postConfigures, IEnumerable<IValidateOptions<TOptions>> validations)
        {
            _config = config;
            _setups = setups;
            _postConfigures = postConfigures;
            _validations = validations;
        }
        public TOptions Create(string name)
        {
            var options = CreateInstance(name);
            foreach (var setup in _setups)
            {
                if (setup is IConfigureNamedOptions<TOptions> namedSetup)
                {
                    namedSetup.Configure(name, options);

                    if (setup is NamedConfigureFromConfigurationOptions<TOptions> configSetup)
                    {
                        var target = configSetup.Action.Target;
                        var fields = target.GetType().GetFields().Select(field => field.GetValue(target)).ToList();
                        var configurationSection = fields.Where(c => c is ConfigurationSection).Select(c => c as ConfigurationSection).FirstOrDefault();

                        if (configurationSection != null)
                        {
                            var tenantSection = _config.GetSection(configurationSection.Path);
                            if (tenantSection != null)
                            {
                                var tenantSetup = new NamedConfigureFromConfigurationOptions<TOptions>(configSetup.Name, tenantSection);
                                tenantSetup.Configure(name, options);
                            }
                        }
                    }
                }
                else if (name == Options.DefaultName)
                {
                    setup.Configure(options);
                }
            }
            foreach (var post in _postConfigures)
            {
                post.PostConfigure(name, options);
            }

            if (_validations != null)
            {
                var failures = new List<string>();
                foreach (var validate in _validations)
                {
                    var result = validate.Validate(name, options);
                    if (result.Failed)
                    {
                        failures.AddRange(result.Failures);
                    }
                }
                if (failures.Count > 0)
                {
                    throw new OptionsValidationException(name, typeof(TOptions), failures);
                }
            }

            return options;
        }

        protected virtual TOptions CreateInstance(string name)
        {
            return Activator.CreateInstance<TOptions>();
        }
    }
}
