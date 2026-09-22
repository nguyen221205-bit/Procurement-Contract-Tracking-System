import time
import json
import statistics
from concurrent.futures import ThreadPoolExecutor, as_completed
import urllib.request
import urllib.error

BASE_URL = "http://localhost:5225"
CONCURRENCY = 50

def get_admin_token():
    url = f"{BASE_URL}/api/auth/login"
    payload = json.dumps({"email": "admin@procurement.com", "password": "Admin@123"}).encode("utf-8")
    req = urllib.request.Request(url, data=payload, headers={"Content-Type": "application/json"}, method="POST")
    with urllib.request.urlopen(req) as resp:
        data = json.loads(resp.read().decode("utf-8"))
        return data["data"]["token"]

def send_request(url, token):
    req = urllib.request.Request(url, headers={"Authorization": f"Bearer {token}", "Accept": "application/json"}, method="GET")
    start = time.perf_counter()
    try:
        with urllib.request.urlopen(req) as resp:
            elapsed_ms = (time.perf_counter() - start) * 1000.0
            return resp.status, elapsed_ms
    except urllib.error.HTTPError as e:
        elapsed_ms = (time.perf_counter() - start) * 1000.0
        return e.code, elapsed_ms
    except Exception as e:
        elapsed_ms = (time.perf_counter() - start) * 1000.0
        return 500, elapsed_ms

def benchmark_endpoint(name, url, token, concurrency=50):
    print(f"\n----------------------------------------------------------------")
    print(f"BENCHMARK: {name}")
    print(f"Target URL: {url}")
    print(f"Firing {concurrency} concurrent requests via ThreadPoolExecutor...")

    overall_start = time.perf_counter()
    latencies = []
    status_codes = []

    with ThreadPoolExecutor(max_workers=concurrency) as executor:
        futures = [executor.submit(send_request, url, token) for _ in range(concurrency)]
        for f in as_completed(futures):
            code, lat = f.result()
            status_codes.append(code)
            latencies.append(lat)

    overall_elapsed = time.perf_counter() - overall_start

    total_requests = len(status_codes)
    success_count = sum(1 for c in status_codes if c == 200)
    fail_count = total_requests - success_count

    sorted_lats = sorted(latencies)
    min_lat = round(sorted_lats[0], 2)
    max_lat = round(sorted_lats[-1], 2)
    avg_lat = round(statistics.mean(latencies), 2)
    p50_lat = round(statistics.median(latencies), 2)
    p95_index = int(len(sorted_lats) * 0.95)
    p95_lat = round(sorted_lats[min(p95_index, len(sorted_lats) - 1)], 2)
    rps = round(total_requests / overall_elapsed, 1)

    print(f"  -> Results for {name}:")
    print(f"     Total Requests  : {total_requests}")
    print(f"     Success (200 OK): {success_count}/{total_requests} ({(success_count/total_requests)*100:.1f}%)")
    print(f"     Failed          : {fail_count}")
    print(f"     Total Duration  : {overall_elapsed:.3f} s")
    print(f"     Throughput      : {rps} Req/sec")
    print(f"     Min Latency     : {min_lat} ms")
    print(f"     Max Latency     : {max_lat} ms")
    print(f"     Avg Latency     : {avg_lat} ms")
    print(f"     P50 (Median)    : {p50_lat} ms")
    print(f"     P95 (Tail)      : {p95_lat} ms")

    return {
        "endpoint": name,
        "requests": total_requests,
        "success": f"{success_count}/{total_requests}",
        "rps": f"{rps} req/s",
        "avg": f"{avg_lat} ms",
        "p50": f"{p50_lat} ms",
        "p95": f"{p95_lat} ms",
        "min_max": f"{min_lat} - {max_lat} ms"
    }

def main():
    print("=" * 64)
    print(" STARTING CONCURRENCY & LOAD BENCHMARK SUITE (TRACK 1 - DEV 1)")
    print("=" * 64)

    token = get_admin_token()
    print("[AUTH] Admin JWT Bearer Token acquired successfully.\n")

    targets = [
        ("GET /api/bid-packages?status=Open", f"{BASE_URL}/api/bid-packages?status=Open"),
        ("GET /api/reports/dashboard", f"{BASE_URL}/api/reports/dashboard"),
        ("GET /api/contracts/contractor/1", f"{BASE_URL}/api/contracts/contractor/1")
    ]

    summary = []
    for name, url in targets:
        res = benchmark_endpoint(name, url, token, concurrency=CONCURRENCY)
        summary.append(res)

    print("\n" + "=" * 64)
    print(" CONCURRENCY & PERFORMANCE BENCHMARK SUMMARY TABLE")
    print("=" * 64)
    print(f"{'Endpoint':<35} | {'Reqs':<5} | {'Success':<8} | {'Throughput':<12} | {'Avg':<10} | {'P50':<10} | {'P95':<10}")
    print("-" * 105)
    for s in summary:
        print(f"{s['endpoint']:<35} | {s['requests']:<5} | {s['success']:<8} | {s['rps']:<12} | {s['avg']:<10} | {s['p50']:<10} | {s['p95']:<10}")

if __name__ == "__main__":
    main()
