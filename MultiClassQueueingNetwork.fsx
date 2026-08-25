(**
# Multi-Class Queueing Network (MCQN) Definition

A Multi-Class Queueing Network is a stochastic network model where multiple distinct 
types of customer classes share a set of service station resources.

Formally, an MCQN is defined as a tuple:
    N = (J, K, alpha, S, P, C)

1. Service Stations (J): A finite set of service servers/queues.
2. Customer Classes (K): A finite set of classes. Each class k belongs to exactly one station J.
3. Exogenous Arrival Vector (alpha): The rate at which external customers enter directly as class k.
4. Service Times Vector (S): Expected service time (m_k) or service rate (mu_k = 1 / m_k).
5. Class Routing Matrix (P): A substochastic matrix where P_{k, k'} is the transition probability.
6. Service Disciplines (C): Scheduling policies enforced at each node (e.g., FIFO, Priority).

The Traffic Equations are governed by:
    lambda_k = alpha_k + Sum( lambda_k' * P_{k', k} )
*)

//namespace QueueingTheory

open System

// --- 1. DOMAIN LAYER TYPES ---

type StationId = StationId of int
type ClassId   = ClassId of int

type ServiceDiscipline =
    | FIFO
    | StaticPriority of ClassId list
    | ProcessorSharing

type Station = {
    Id: StationId
    Name: string
    Discipline: ServiceDiscipline
}

type CustomerClass = {
    Id: ClassId
    StationId: StationId 
    ExogenousArrivalRate: double
    MeanServiceTime: double
}

type QueueingNetwork = {
    Stations: Map<StationId, Station>
    Classes: Map<ClassId, CustomerClass>
    RoutingMatrix: Map<ClassId, Map<ClassId, double>>
}

// --- 2. MATHEMATICAL LOGIC MODULE ---

module QueueingNetwork =

    /// Computes total arrival rates (Lambda) for each class using fixed-point iteration
    let computeTrafficRates (network: QueueingNetwork) (tolerance: double) : Map<ClassId, double> =
        let classes = network.Classes |> Map.values |> Seq.toList
        let initialLambdas = classes |> List.map (fun c -> c.Id, c.ExogenousArrivalRate) |> Map.ofList

        let rec step (currentLambdas: Map<ClassId, double>) =
            let nextLambdas =
                classes 
                |> List.map (fun targetClass ->
                    let alpha = targetClass.ExogenousArrivalRate
                    let internalArrivals =
                        classes
                        |> List.sumBy (fun sourceClass ->
                            let lambdaSource = Map.find sourceClass.Id currentLambdas
                            let p = 
                                network.RoutingMatrix 
                                |> Map.tryFind sourceClass.Id 
                                |> Option.bind (Map.tryFind targetClass.Id)
                                |> Option.defaultValue 0.0
                            lambdaSource * p
                        )
                    targetClass.Id, alpha + internalArrivals
                )
                |> Map.ofList

            let maxDelta = 
                classes 
                |> List.map (fun c -> abs (Map.find c.Id nextLambdas - Map.find c.Id currentLambdas))
                |> List.max

            if maxDelta < tolerance then nextLambdas else step nextLambdas

        step initialLambdas

    /// Computes the total traffic utilization (Rho) for every server station
    let computeStationUtilizations (network: QueueingNetwork) (lambdas: Map<ClassId, double>) : Map<StationId, double> =
        network.Classes 
        |> Map.values
        |> Seq.groupBy (fun c -> c.StationId)
        |> Seq.map (fun (stationId, stationClasses) ->
            let rho = 
                stationClasses 
                |> Seq.sumBy (fun c -> 
                    let lambda = Map.find c.Id lambdas
                    lambda * c.MeanServiceTime
                )
            stationId, rho
        )
        |> Map.ofSeq

// --- 3. EXECUTION SANDBOX ---

// Define network entities
let station1 = { Id = StationId 1; Name = "Server A"; Discipline = FIFO }
let station2 = { Id = StationId 2; Name = "Server B"; Discipline = ProcessorSharing }

let class1 = { Id = ClassId 1; StationId = StationId 1; ExogenousArrivalRate = 0.5; MeanServiceTime = 0.8 }
let class2 = { Id = ClassId 2; StationId = StationId 2; ExogenousArrivalRate = 0.0; MeanServiceTime = 1.2 }

let routing = 
    Map [
        ClassId 1, Map [ ClassId 2, 0.5 ]
        ClassId 2, Map [ ClassId 1, 0.2 ]
    ]

let myNetwork = {
    Stations = Map [ station1.Id, station1; station2.Id, station2 ]
    Classes = Map [ class1.Id, class1; class2.Id, class2 ]
    RoutingMatrix = routing
}

// Run math logic
let lambdas = QueueingNetwork.computeTrafficRates myNetwork 0.0001
let rhos = QueueingNetwork.computeStationUtilizations myNetwork lambdas

// Output results
printfn "--- NETWORK RESULTS ---"
lambdas |> Map.iter (fun (ClassId id) rate -> printfn "Class %d Total Arrival Rate (λ): %.4f" id rate)
rhos |> Map.iter (fun (StationId id) utilization -> printfn "Station %d Utilization (ρ): %.4f" id utilization)

