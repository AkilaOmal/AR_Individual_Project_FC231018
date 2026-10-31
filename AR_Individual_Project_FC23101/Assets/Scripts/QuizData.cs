public class QuizQuestion
{
    public string question;
    public string correct;
    public string wrong;
    public string explanation;

    public QuizQuestion(string question, string correct, string wrong, string explanation)
    {
        this.question = question;
        this.correct = correct;
        this.wrong = wrong;
        this.explanation = explanation;
    }
}

public static class QuizData
{
    private static readonly string[] Titles = new string[]
    {
        "Module 1: Internet City",
        "Module 2: Data Packet Journey",
        "Module 3: REST vs GraphQL",
        "Module 4: Monolith vs Microservices",
        "Module 5: HTTP vs WebSocket"
    };

    public static string TitleFor(int index)
    {
        if (index < 0 || index >= Titles.Length) return "Quiz";
        return Titles[index];
    }

    public static QuizQuestion[] ForModule(int index)
    {
        switch (index)
        {
            case 0: return Module1();
            case 1: return Module2();
            case 2: return Module3();
            case 3: return Module4();
            case 4: return Module5();
            default: return null;
        }
    }

    // ---------- Module 1: Internet City ----------
    private static QuizQuestion[] Module1()
    {
        return new QuizQuestion[]
        {
            new QuizQuestion(
                "Which component shares incoming requests between several servers?",
                "Load balancer",
                "Database",
                "A load balancer distributes requests so no single server takes all the traffic."),

            new QuizQuestion(
                "What can happen when too many requests reach one server?",
                "It can become overloaded and slow down or fail",
                "It automatically becomes faster",
                "A server has limited capacity, so heavy traffic causes delays or failed requests."),

            new QuizQuestion(
                "In the demo, how did the client get course data from the database?",
                "Through the server, which queried the database",
                "By connecting directly to the database",
                "The client talks to the server, and the server accesses the database."),

            new QuizQuestion(
                "Who starts a basic request-response exchange?",
                "The client",
                "The database",
                "The client sends a request and the server sends back a response."),

            new QuizQuestion(
                "Why didn't adding servers alone fix the overload?",
                "Requests still went to Server 1 until the load balancer was enabled",
                "Extra servers cannot handle requests",
                "Something has to spread the requests across the servers. That is the load balancer's job.")
        };
    }

    // ---------- Module 2: Data Packet Journey ----------
    private static QuizQuestion[] Module2()
    {
        return new QuizQuestion[]
        {
            new QuizQuestion(
                "Which HTTP method is normally used to create a new resource?",
                "POST",
                "GET",
                "POST creates a resource, while GET only retrieves data."),

            new QuizQuestion(
                "What does the status code 404 mean?",
                "The requested resource was not found",
                "The server crashed",
                "404 Not Found means the server could not find what was requested."),

            new QuizQuestion(
                "Which data format carried the response body in the demo?",
                "JSON",
                "A video file",
                "The server returned the course list as JSON text."),

            new QuizQuestion(
                "Which status codes indicate a server error?",
                "5xx, such as 500",
                "2xx, such as 200",
                "5xx codes mean the server failed. 2xx codes mean success."),

            new QuizQuestion(
                "Why did the 500 error never reach the database in the demo?",
                "The server failed while handling the request",
                "The client blocked the request",
                "The failure happened inside the server before the database step.")
        };
    }

    // ---------- Module 3: REST vs GraphQL ----------
    private static QuizQuestion[] Module3()
    {
        return new QuizQuestion[]
        {
            new QuizQuestion(
                "How many requests did REST use for the profile and the courses in the demo?",
                "Two",
                "One",
                "One request fetched the profile and a second request fetched the courses."),

            new QuizQuestion(
                "What does a GraphQL query specify?",
                "The exact fields the client wants",
                "Which server hardware to use",
                "The query lists the fields, and the response contains those fields."),

            new QuizQuestion(
                "How many endpoints does a GraphQL API usually expose?",
                "Usually a single endpoint",
                "A separate endpoint for every field",
                "The client sends its query to one GraphQL endpoint."),

            new QuizQuestion(
                "Can a REST API return related data in one request?",
                "Yes, if its endpoints are designed that way",
                "No, never",
                "What a REST response contains depends on how each endpoint is designed."),

            new QuizQuestion(
                "Which style is built around resource-oriented endpoints?",
                "REST",
                "GraphQL",
                "REST organizes access around resources, such as /students/1.")
        };
    }

    // ---------- Module 4: Monolith vs Microservices ----------
    private static QuizQuestion[] Module4()
    {
        return new QuizQuestion[]
        {
            new QuizQuestion(
                "How is a monolithic application deployed?",
                "As one deployable application",
                "As many independent services",
                "In a monolith the functions run together in a single application."),

            new QuizQuestion(
                "What does the API Gateway do in the demo?",
                "Routes requests to the right service",
                "Stores the course data",
                "The gateway is the front door that sends each request to the service that handles it."),

            new QuizQuestion(
                "What happened when the Notification Service failed in microservices?",
                "The other services kept working",
                "Every service stopped immediately",
                "Separate services can isolate a failure, though failures can still spread when services depend on each other."),

            new QuizQuestion(
                "Which design lets you scale only the Payment part?",
                "Microservices",
                "A monolith",
                "Individual services can be scaled separately. A monolith is usually scaled as a whole."),

            new QuizQuestion(
                "What extra complexity do microservices add?",
                "Services must communicate over a network",
                "Parts can never talk to each other",
                "A distributed system brings network communication and coordination challenges.")
        };
    }

    // ---------- Module 5: HTTP vs WebSocket ----------
    private static QuizQuestion[] Module5()
    {
        return new QuizQuestion[]
        {
            new QuizQuestion(
                "In plain HTTP, who normally starts an exchange?",
                "The client",
                "The server",
                "The client sends a request and the server answers it."),

            new QuizQuestion(
                "What is polling?",
                "The client repeatedly asking whether there is anything new",
                "The server sending data without being asked",
                "Polling means asking again and again, because the server cannot announce news on its own in plain HTTP."),

            new QuizQuestion(
                "What can happen after a WebSocket handshake succeeds?",
                "The connection stays open for two-way messages",
                "The connection closes after one message",
                "After the upgrade, both sides can send messages over the open connection."),

            new QuizQuestion(
                "Which suits a live chat better?",
                "WebSocket",
                "A one-time page load",
                "A persistent two-way connection lets messages arrive as soon as they are sent."),

            new QuizQuestion(
                "Why did Client A see Client B's message late in HTTP mode?",
                "It only found out on its next poll",
                "The server deleted the message",
                "In HTTP the receiver has to ask, so delivery waited for the next poll.")
        };
    }
}