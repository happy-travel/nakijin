# nakijin - Mapper
The project contains API methods to get and manage locations to be used by other services: locations, hotels, etc.

### Infrastructure Dependencies
* Database connection string
* Redis instance
* Sentry endpoint
* Access to Vault
* Jaeger instance _(optional)_

### Project Dependencies
Libraries that project relies on:
* [Edo.Contracts](https://github.com/happy-travel/edo-contracts)
* [Multilanguage](https://github.com/happy-travel/multi-language) 
* [LocationNameNormalizer](https://github.com/happy-travel/location-name-normalizer)

Libraries' versions _must_ be in sync with other services.
Also you might need NetTopologySuite and enabled GIS services on the DB side.
