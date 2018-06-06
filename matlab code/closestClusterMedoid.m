function [ closestMedoidIdx, closestMedoidDistance] = closestClusterMedoid( sample, medoids )
%UNTITLED3 Summary of this function goes here
%   Detailed explanation goes here
%        distA = distBW2obs(sample(1,1:2),medoids(1,1:2));
%        distB = distBW2obs(sample(1,1:2),medoids(2,1:2));
%        distC = distBW2obs(sample(1,1:2),medoids(3,1:2));
       distMedoid = zeros(size(medoids,1),1);
       for i = 1:size(medoids,1)
           distMedoid(i) = distBW2obs(sample(1,1:2),medoids(i,1:2));
       end
%     closestMedoid = min(distBW2obs(sample(1,1:2),medoids(1,1:2)),distBW2obs(sample(1,1:2),medoids(2,1:2)),distBW2obs(sample(1,1:2),medoids(3,1:2)));
      [closestMedoidDistance, closestMedoidIdx] = min(distMedoid);

end

