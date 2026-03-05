# North Yorkshire Bin Collections Home Assistant Integration

v0.0.1 - Initial release, basic functionality working, lots to improve.

A self-hosted .NET 8 service that scrapes bin collection schedules from the North Yorkshire Council website and publishes them to Home Assistant via MQTT auto-discovery.

### Run order
1. Test the MQTT broker connection.
2. Wait until the next scheduled cron time.
3. Get the bin collection data from the council website.
4. Publish MQTT discovery configuration so Home Assistant auto-creates the sensors.
5. Publish the current state values to the sensor topics.

## Sensors

The service automatically registers three sensors in Home Assistant:

| Sensor | Entity ID | Description |
|---|---|---|
| Next Bin Type | `sensor.next_bin_type` | The type of bin being collected next (e.g. "General Waste", "Recycling") |
| Next Collection Timestamp | `sensor.next_bin_collection_timestamp` | The date/time of the next collection |
| Future Bin Type | `sensor.future_bin_type` | The type of bin being collected after the next one |

These are published using Home Assistant's [MQTT Discovery](https://www.home-assistant.io/integrations/mqtt/#mqtt-discovery) protocol, so they appear automatically under a **North Yorkshire Bin Collections** device — no manual YAML configuration needed.

## Prerequisites
- An MQTT broker (e.g. [Mosquitto](https://mosquitto.org/))
- Home Assistant with the [MQTT integration](https://www.home-assistant.io/integrations/mqtt/) configured and connected to the same broker
- Your North Yorkshire Council property ID (see [Finding Your Property ID](#finding-your-property-id))

## Finding Your Property ID

The service needs your unique property ID to look up your bin schedule on the North Yorkshire Council website.

1. Go to [https://www.northyorks.gov.uk/bin-calendar/lookup](https://www.northyorks.gov.uk/bin-calendar/lookup)
2. Enter your postcode/address to find your property.
3. Once your bin schedule is displayed, look at the URL in your browser's address bar. It will look something like:
   ```
   https://www.northyorks.gov.uk/bin-calendar/Hambleton/results/123456
   ```
4. The number at the end of the URL (`123456` in this example) is your **property ID**.

## Configuration

1. Copy the template configuration file:
   ```bash
   cp northyorks-bin-collections/appsettings.template.json northyorks-bin-collections/appsettings.json
   ```

2. Edit `appsettings.json` with your details:
   ```json
   {
     "Mqtt": {
       "BrokerHost": "<your-mqtt-broker-host>",
       "BrokerPort": 1883,
       "Username": "<your-mqtt-username>",
       "Password": "<your-mqtt-password>",
       "ClientId": "northyorks-bin-collections"
     },
     "BinCollection": {
       "Url": "https://www.northyorks.gov.uk/bin-calendar/Hambleton/results/<YOUR-PROPERTY-ID>/ajax?_wrapper_format=drupal_ajax",
       "ReferrerUrl": "https://www.northyorks.gov.uk/bin-calendar/Hambleton/results/<YOUR-PROPERTY-ID>"
     },
     "Schedule": {
       "CronExpression": "0 1 * * *"
     },
     "HomeAssistant": {
       "DeviceId": "northyorks_bin_collections",
       "SensorPrefix": "homeassistant/sensor"
     }
   }
   ```

### Configuration

| Section | Key | Description |
|---|---|---|
| `Mqtt` | `BrokerHost` | Hostname or IP address of your MQTT broker |
| `Mqtt` | `BrokerPort` | MQTT broker port (default `1883`) |
| `Mqtt` | `Username` | MQTT username |
| `Mqtt` | `Password` | MQTT password |
| `Mqtt` | `ClientId` | MQTT client identifier (default `northyorks-bin-collections`) |
| `BinCollection` | `Url` | The North Yorkshire Council AJAX endpoint — replace `<YOUR-PROPERTY-ID>` with your property ID |
| `BinCollection` | `ReferrerUrl` | The referrer URL — replace `<YOUR-PROPERTY-ID>` with your property ID |
| `Schedule` | `CronExpression` | A cron expression controlling how often the service fetches new data (default `0 1 * * *` — daily at 01:00) |
| `HomeAssistant` | `DeviceId` | The device identifier used in Home Assistant (default `northyorks_bin_collections`) |
| `HomeAssistant` | `SensorPrefix` | The MQTT topic prefix for HA discovery (default `homeassistant/sensor`) |

---

## Hosting

This service is designed to run as a background process on a self-hosted server. Below are some common deployment approaches.

### Quick Start
#### LXC
`TODO`
#### Docker
`TODO`

### Manual

#### LXC Container

1. Create a new **Debian** LXC container in Proxmox.
   - 512 MB RAM and 1 CPU core is more than sufficient.
2. Install the .NET 8 runtime inside the container:
   ```bash
   apt update && apt install -y dotnet-runtime-8.0
   ```
3. Copy the published application into the container:
   ```bash
   # On your build machine
   cd northyorks-bin-collections
   dotnet publish -c Release -o ./publish

   # Copy the publish folder to the LXC container
   scp -r ./publish root@<container-ip>:/opt/northyorks-bin-collections
   ```
4. Ensure your `appsettings.json` is present in the publish output directory.
5. Create a systemd service so it starts automatically:
   ```ini
   # /etc/systemd/system/northyorks-bin-collections.service
   [Unit]
   Description=North Yorkshire Bin Collections
   After=network.target

   [Service]
   Type=simple
   WorkingDirectory=/opt/northyorks-bin-collections
   ExecStart=/usr/bin/dotnet /opt/northyorks-bin-collections/northyorks-bin-collections.dll
   Restart=always
   RestartSec=10

   [Install]
   WantedBy=multi-user.target
   ```
6. Enable and start the service:
   ```bash
   systemctl daemon-reload
   systemctl enable northyorks-bin-collections
   systemctl start northyorks-bin-collections
   systemctl status northyorks-bin-collections
   ```

#### Docker
`TODO`

## Home Assistant Setup

Once the service is running and connected to your MQTT broker:

1. Ensure the **MQTT** integration is installed and configured in Home Assistant, pointing at the same broker.
2. The sensors will appear automatically under **Settings → Devices & Services → MQTT → North Yorkshire Bin Collections**.
3. No manual `sensor:` YAML configuration is required — the service uses MQTT auto-discovery.

You can then use the sensors in automations, dashboards, or cards like any other Home Assistant entity.
