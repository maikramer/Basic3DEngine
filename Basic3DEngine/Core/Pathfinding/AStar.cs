using System.Collections.Generic;
using System.Numerics;

namespace Basic3DEngine.Core.Pathfinding;

/// <summary>
/// Implementação simples de A* em uma grade 2D (top-down).
/// Células walkable = verdadeiras. Retorna caminho como lista de posições (em coordenadas de célula).
/// </summary>
public static class AStar
{
    private sealed class Node
    {
        public int X;
        public int Y;
        public float G; // custo do início até este nó
        public float H; // heuristic
        public float F => G + H;
        public Node Parent;
    }

    /// <summary>
    /// Encontra um caminho do início ao fim em uma grade booleana.
    /// </summary>
    /// <param name="walkable">Matriz booleana onde true = célula caminhável.</param>
    /// <param name="start">Posição inicial (índices de célula).</param>
    /// <param name="goal">Posição alvo (índices de célula).</param>
    /// <param name="allowDiagonals">Se verdadeiro, considera 8 vizinhos.</param>
    /// <returns>Lista de pontos (x, y) de start a goal, ou lista vazia se não houver caminho.</returns>
    public static List<Vector2> FindPath(bool[,] walkable, Vector2 start, Vector2 goal, bool allowDiagonals = false)
    {
        int width = walkable.GetLength(0);
        int height = walkable.GetLength(1);

        var open = new List<Node>();
        var openSet = new HashSet<(int,int)>();
        var closedSet = new HashSet<(int,int)>();

        var startNode = new Node { X = (int)start.X, Y = (int)start.Y, G = 0, H = Heuristic(start, goal) };
        open.Add(startNode);
        openSet.Add((startNode.X, startNode.Y));

        while (open.Count > 0)
        {
            // extrair nó com menor F
            int bestIndex = 0;
            float bestF = open[0].F;
            for (int i = 1; i < open.Count; i++)
            {
                if (open[i].F < bestF)
                {
                    bestF = open[i].F;
                    bestIndex = i;
                }
            }

            var current = open[bestIndex];
            open.RemoveAt(bestIndex);
            openSet.Remove((current.X, current.Y));
            closedSet.Add((current.X, current.Y));

            if (current.X == (int)goal.X && current.Y == (int)goal.Y)
            {
                return ReconstructPath(current);
            }

            foreach (var (nx, ny, cost) in GetNeighbors(current.X, current.Y, width, height, allowDiagonals))
            {
                if (!walkable[nx, ny] || closedSet.Contains((nx, ny)))
                    continue;

                float tentativeG = current.G + cost;
                Node neighbor = null;
                for (int i = 0; i < open.Count; i++)
                {
                    if (open[i].X == nx && open[i].Y == ny)
                    {
                        neighbor = open[i];
                        break;
                    }
                }

                if (neighbor == null)
                {
                    neighbor = new Node { X = nx, Y = ny };
                    neighbor.G = tentativeG;
                    neighbor.H = Heuristic(new Vector2(nx, ny), goal);
                    neighbor.Parent = current;
                    open.Add(neighbor);
                    openSet.Add((nx, ny));
                }
                else if (tentativeG < neighbor.G)
                {
                    neighbor.G = tentativeG;
                    neighbor.Parent = current;
                }
            }
        }

        return new List<Vector2>();
    }

    private static float Heuristic(Vector2 a, Vector2 b)
    {
        // distância Manhattan
        return MathF.Abs(a.X - b.X) + MathF.Abs(a.Y - b.Y);
    }

    private static IEnumerable<(int x, int y, float cost)> GetNeighbors(int x, int y, int width, int height, bool diag)
    {
        // 4 vizinhos
        var deltas = new (int dx, int dy, float cost)[]
        {
            (1, 0, 1f), (-1, 0, 1f), (0, 1, 1f), (0, -1, 1f)
        };

        for (int i = 0; i < deltas.Length; i++)
        {
            int nx = x + deltas[i].dx;
            int ny = y + deltas[i].dy;
            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                yield return (nx, ny, deltas[i].cost);
        }

        if (diag)
        {
            var diagonals = new (int dx, int dy, float cost)[]
            {
                (1, 1, 1.4142f), (1, -1, 1.4142f), (-1, 1, 1.4142f), (-1, -1, 1.4142f)
            };
            for (int i = 0; i < diagonals.Length; i++)
            {
                int nx = x + diagonals[i].dx;
                int ny = y + diagonals[i].dy;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    yield return (nx, ny, diagonals[i].cost);
            }
        }
    }

    private static List<Vector2> ReconstructPath(Node node)
    {
        var path = new List<Vector2>();
        var current = node;
        while (current != null)
        {
            path.Add(new Vector2(current.X, current.Y));
            current = current.Parent;
        }
        path.Reverse();
        return path;
    }
}


