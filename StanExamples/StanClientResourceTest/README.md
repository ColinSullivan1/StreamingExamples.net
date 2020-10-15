#  The Stan Client Resource Test

To better diagnose errors, the StanClientResource test can be done to test
basic resilience ofthe NATS streaming .NET client.

The basic flow is a producer asyncyronously sends messages with a payload
of 15k while limiting outstanding acks.  This provides decent but not
overwhelming load for a long running test.

A subscriber is setup to receive messages and output throughput staticstics.

Every three seconds, various memory and CPU utilization statistics are printed.

## Usage

A url and cluster ID can be specified.  The default url is localhost (so it
won't work with docker), and the default cluster ID is `test-cluster`.

### Command Line

`dotnet run <optional url> <cluster id>`

### Docker

There's a docker file available to build your own image, and a k8s deployment.

`docker run csullivan9999/netstanres <required url> <cluster id>`

Note the url is required here as the default localhost/loopback won't work
inside the docker container.

## Output

Example output:

```text
10/15/2020 19:28:25: ==== STAN Client Resource Test ====
10/15/2020 19:28:25: Using Url: nats://stan:4222
10/15/2020 19:28:25: Using ClusterID: stan
10/15/2020 19:28:25: Starting Subscriber.
10/15/2020 19:28:26: Starting Publisher.
10/15/2020 19:28:26: Publisher Sent 0 messages to the streaming server.
10/15/2020 19:28:26: Publisher Sent 500 messages to the streaming server.
10/15/2020 19:28:26: Subscriber Received 500 msgs at 856 msgs/sec.
10/15/2020 19:28:27: Publisher Sent 1000 messages to the streaming server.
10/15/2020 19:28:27: Subscriber Received 1000 msgs at 868 msgs/sec.
10/15/2020 19:28:27: Publisher Sent 1500 messages to the streaming server.

...

10/15/2020 19:45:39: Publisher Sent 1011000 messages to the streaming server.
10/15/2020 19:45:39: Subscriber Received 1011000 msgs at 979 msgs/sec.
10/15/2020 19:45:39: Publisher Sent 1011500 messages to the streaming server.
10/15/2020 19:45:39: Subscriber Received 1011500 msgs at 979 msgs/sec.
10/15/2020 19:45:40: Publisher Sent 1012000 messages to the streaming server.
10/15/2020 19:45:40: Subscriber Received 1012000 msgs at 979 msgs/sec.
10/15/2020 19:45:40: Publisher Sent 1012500 messages to the streaming server.
10/15/2020 19:45:40: System Resources:
10/15/2020 19:45:40:    Working set  : 59015168 bytes
10/15/2020 19:45:40:    Paged Memory : 0 bytes
10/15/2020 19:45:40:    GC Memory    : 6135552 bytes
10/15/2020 19:45:40:    CPU Time     : 156.43 sec
10/15/2020 19:45:40:    CPU Percent  : 72.6 %
10/15/2020 19:45:40: Subscriber Received 1012500 msgs at 979 msgs/sec.
10/15/2020 19:45:41: Publisher Sent 1013000 messages to the streaming server.
10/15/2020 19:45:41: Subscriber Received 1013000 msgs at 979 msgs/sec.
10/15/2020 19:45:41: Publisher Sent 1013500 messages to the streaming server.
10/15/2020 19:45:41: Subscriber Received 1013500 msgs at 979 msgs/sec.
10/15/2020 19:45:41: Publisher Sent 1014000 messages to the streaming server.
10/15/2020 19:45:42: Subscriber Received 1014000 msgs at 979 msgs/sec.
10/15/2020 19:45:42: Publisher Sent 1014500 messages to the streaming server.
10/15/2020 19:45:42: Subscriber Received 1014500 msgs at 979 msgs/sec.
10/15/2020 19:45:42: Publisher Sent 1015000 messages to the streaming server.
10/15/2020 19:45:43: Subscriber Received 1015000 msgs at 979 msgs/sec.
10/15/2020 19:45:43: Publisher Sent 1015500 messages to the streaming server.
10/15/2020 19:45:43: Subscriber Received 1015500 msgs at 979 msgs/sec.
10/15/2020 19:45:43: Publisher Sent 1016000 messages to the streaming server.
10/15/2020 19:45:43: System Resources:
10/15/2020 19:45:43:    Working set  : 59015168 bytes
10/15/2020 19:45:43:    Paged Memory : 0 bytes
10/15/2020 19:45:43:    GC Memory    : 10533608 bytes
10/15/2020 19:45:43:    CPU Time     : 156.97 sec
10/15/2020 19:45:43:    CPU Percent  : 79.2 %
10/15/2020 19:45:43: Subscriber Received 1016000 msgs at 979 msgs/sec.
10/15/2020 19:45:44: Publisher Sent 1016500 messages to the streaming server.
10/15/2020 19:45:44: Subscriber Received 1016500 msgs at 979 msgs/sec.
10/15/2020 19:45:44: Publisher Sent 1017000 messages to the streaming server.
10/15/2020 19:45:44: Subscriber Received 1017000 msgs at 979 msgs/sec.
10/15/2020 19:45:45: Publisher Sent 1017500 messages to the streaming server.
10/15/2020 19:45:45: Subscriber Received 1017500 msgs at 979 msgs/sec.

```